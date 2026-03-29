#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class WarningPointsService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public WarningPointsService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<(WarningPoint Warning, int TotalPoints, string AutoAction)> AddWarningAsync(
        ulong guildId, ulong userId, ulong modId, string severity, string reason)
    {
        int points = severity.ToLowerInvariant() switch
        {
            "minor" => 1,
            "major" => 3,
            _ => int.TryParse(severity, out var custom) && custom > 0 ? custom : 1
        };

        await using var ctx = _db.GetDbContext();

        var id = await ctx.GetTable<WarningPoint>()
            .InsertWithInt32IdentityAsync(() => new WarningPoint
            {
                GuildId = guildId,
                UserId = userId,
                ModeratorId = modId,
                Reason = reason ?? "No reason provided",
                Points = points,
                Severity = severity.ToLowerInvariant()
            });

        var totalPoints = await ctx.GetTable<WarningPoint>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .SumAsyncLinqToDB(x => x.Points);

        // Check thresholds
        var configs = await ctx.GetTable<WarningPointConfig>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Threshold)
            .ToListAsyncLinqToDB();

        string autoAction = null;
        foreach (var cfg in configs)
        {
            if (totalPoints >= cfg.Threshold)
            {
                autoAction = cfg.Action;
                break;
            }
        }

        return (new WarningPoint { Id = id, Points = points, Severity = severity }, totalPoints, autoAction);
    }

    public async Task<bool> SetThresholdAsync(ulong guildId, int threshold, string action)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<WarningPointConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Threshold == threshold);

        if (existing is not null)
        {
            await ctx.GetTable<WarningPointConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new WarningPointConfig { Action = action });
        }
        else
        {
            await ctx.GetTable<WarningPointConfig>()
                .InsertAsync(() => new WarningPointConfig
                {
                    GuildId = guildId,
                    Threshold = threshold,
                    Action = action
                });
        }

        return true;
    }

    public async Task<(List<WarningPoint> Warnings, int TotalPoints)> GetStatusAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var warnings = await ctx.GetTable<WarningPoint>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .OrderByDescending(x => x.DateAdded)
            .ToListAsyncLinqToDB();

        var total = warnings.Sum(x => x.Points);
        return (warnings, total);
    }
}
