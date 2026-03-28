using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class ScheduledRoleService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public ScheduledRoleService(DbService db, DiscordSocketClient client, IBotCreds creds)
    {
        _db = db;
        _client = client;
        _creds = creds;
    }

    public async Task OnReadyAsync()
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await ExecuteDueGrantsAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in scheduled role grant loop");
            }
        }
    }

    private async Task ExecuteDueGrantsAsync()
    {
        await using var ctx = _db.GetDbContext();
        var now = DateTime.UtcNow;

        var dueGrants = await ctx.GetTable<ScheduledRoleGrant>()
            .Where(x => !x.Executed && x.ScheduledFor <= now)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var grant in dueGrants)
        {
            try
            {
                var guild = _client.GetGuild(grant.GuildId);
                if (guild is null)
                    continue;

                var user = guild.GetUser(grant.UserId);
                var role = guild.GetRole(grant.RoleId);

                if (user is not null && role is not null)
                {
                    if (grant.IsGrant)
                        await user.AddRoleAsync(role);
                    else
                        await user.RemoveRoleAsync(role);

                    Log.Information("Scheduled role {Action} executed: {Role} for {User} in {Guild}",
                        grant.IsGrant ? "grant" : "removal",
                        role.Name, user.Username, guild.Name);
                }

                // Mark as executed regardless (user may have left, role may be deleted)
                await ctx.GetTable<ScheduledRoleGrant>()
                    .Where(x => x.Id == grant.Id)
                    .UpdateAsync(x => new ScheduledRoleGrant { Executed = true });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error executing scheduled role grant {Id}", grant.Id);
            }
        }
    }

    public async Task<ScheduledRoleGrant> ScheduleAsync(
        ulong guildId, ulong userId, ulong roleId, bool isGrant, DateTime scheduledFor, ulong scheduledBy)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ScheduledRoleGrant>()
            .InsertWithOutputAsync(() => new ScheduledRoleGrant
            {
                GuildId = guildId,
                UserId = userId,
                RoleId = roleId,
                IsGrant = isGrant,
                ScheduledFor = scheduledFor,
                Executed = false,
                ScheduledByUserId = scheduledBy,
            });
    }

    public async Task<List<ScheduledRoleGrant>> GetPendingAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ScheduledRoleGrant>()
            .Where(x => x.GuildId == guildId && !x.Executed)
            .OrderBy(x => x.ScheduledFor)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> CancelAsync(ulong guildId, int id)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<ScheduledRoleGrant>()
            .DeleteAsync(x => x.GuildId == guildId && x.Id == id && !x.Executed);
        return deleted > 0;
    }

    public static TimeSpan? ParseDuration(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim().ToLowerInvariant();

        if (input.EndsWith('d') && int.TryParse(input[..^1], out var days))
            return TimeSpan.FromDays(days);
        if (input.EndsWith('h') && int.TryParse(input[..^1], out var hours))
            return TimeSpan.FromHours(hours);
        if (input.EndsWith('m') && int.TryParse(input[..^1], out var minutes))
            return TimeSpan.FromMinutes(minutes);

        return null;
    }
}
