using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class ModAnalyticsService : INService
{
    private readonly DbService _db;

    public ModAnalyticsService(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Record a moderation action (called by other services when mods take action).
    /// </summary>
    public async Task RecordActionAsync(ulong guildId, ulong moderatorId, string actionType, ulong targetUserId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<ModAction>().InsertAsync(() => new ModAction
        {
            GuildId = guildId,
            ModeratorUserId = moderatorId,
            ActionType = actionType,
            TargetUserId = targetUserId,
            ActionAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get a summary of mod actions for a guild within the given number of days.
    /// </summary>
    public async Task<ModStatsSummary> GetGuildStatsAsync(ulong guildId, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        await using var ctx = _db.GetDbContext();
        var actions = await ctx.GetTable<ModAction>()
            .Where(x => x.GuildId == guildId && x.ActionAt >= cutoff)
            .ToListAsyncLinqToDB();

        return new ModStatsSummary
        {
            Days = days,
            TotalActions = actions.Count,
            Warns = actions.Count(x => x.ActionType == "Warn"),
            Mutes = actions.Count(x => x.ActionType == "Mute"),
            Kicks = actions.Count(x => x.ActionType == "Kick"),
            Bans = actions.Count(x => x.ActionType == "Ban"),
            Timeouts = actions.Count(x => x.ActionType == "Timeout"),
            UniqueMods = actions.Select(x => x.ModeratorUserId).Distinct().Count(),
            UniqueTargets = actions.Select(x => x.TargetUserId).Distinct().Count()
        };
    }

    /// <summary>
    /// Get stats for a specific moderator.
    /// </summary>
    public async Task<ModStatsSummary> GetModStatsAsync(ulong guildId, ulong moderatorId, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        await using var ctx = _db.GetDbContext();
        var actions = await ctx.GetTable<ModAction>()
            .Where(x => x.GuildId == guildId && x.ModeratorUserId == moderatorId && x.ActionAt >= cutoff)
            .ToListAsyncLinqToDB();

        return new ModStatsSummary
        {
            Days = days,
            TotalActions = actions.Count,
            Warns = actions.Count(x => x.ActionType == "Warn"),
            Mutes = actions.Count(x => x.ActionType == "Mute"),
            Kicks = actions.Count(x => x.ActionType == "Kick"),
            Bans = actions.Count(x => x.ActionType == "Ban"),
            Timeouts = actions.Count(x => x.ActionType == "Timeout"),
            UniqueMods = 1,
            UniqueTargets = actions.Select(x => x.TargetUserId).Distinct().Count()
        };
    }

    /// <summary>
    /// Get the top moderators by action count.
    /// </summary>
    public async Task<List<(ulong ModId, int Count)>> GetTopModsAsync(ulong guildId, int days = 30, int limit = 10)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        await using var ctx = _db.GetDbContext();
        var results = await ctx.GetTable<ModAction>()
            .Where(x => x.GuildId == guildId && x.ActionAt >= cutoff)
            .GroupBy(x => x.ModeratorUserId)
            .Select(g => new { ModId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsyncLinqToDB();

        return results.Select(x => (x.ModId, x.Count)).ToList();
    }
}

public class ModStatsSummary
{
    public int Days { get; set; }
    public int TotalActions { get; set; }
    public int Warns { get; set; }
    public int Mutes { get; set; }
    public int Kicks { get; set; }
    public int Bans { get; set; }
    public int Timeouts { get; set; }
    public int UniqueMods { get; set; }
    public int UniqueTargets { get; set; }
}
