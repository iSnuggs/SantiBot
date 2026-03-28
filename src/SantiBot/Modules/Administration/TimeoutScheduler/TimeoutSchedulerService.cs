using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class TimeoutSchedulerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;

    public TimeoutSchedulerService(DbService db, DiscordSocketClient client, IBotCreds creds)
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
                await ExecuteDueTimeoutsAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in timeout scheduler loop");
            }
        }
    }

    private async Task ExecuteDueTimeoutsAsync()
    {
        await using var ctx = _db.GetDbContext();

        var now = DateTime.UtcNow;
        var dueTimeouts = await ctx.GetTable<ScheduledTimeout>()
            .Where(x => !x.Executed)
            .Where(x => x.ScheduledFor <= now)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var timeout in dueTimeouts)
        {
            try
            {
                var guild = _client.GetGuild(timeout.GuildId);
                var user = guild?.GetUser(timeout.TargetUserId);

                if (user is not null)
                {
                    var duration = TimeSpan.FromMinutes(timeout.DurationMinutes);
                    await user.SetTimeOutAsync(duration,
                        new RequestOptions { AuditLogReason = $"Scheduled timeout: {timeout.Reason}" });

                    Log.Information("Executed scheduled timeout for {User} in {Guild} (duration: {Duration}m, reason: {Reason})",
                        user.Username, guild?.Name, timeout.DurationMinutes, timeout.Reason);
                }

                // Mark as executed regardless (user may have left)
                await ctx.GetTable<ScheduledTimeout>()
                    .Where(x => x.Id == timeout.Id)
                    .Set(x => x.Executed, true)
                    .UpdateAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error executing scheduled timeout {Id} in guild {GuildId}",
                    timeout.Id, timeout.GuildId);

                // Mark as executed to avoid retrying indefinitely
                await ctx.GetTable<ScheduledTimeout>()
                    .Where(x => x.Id == timeout.Id)
                    .Set(x => x.Executed, true)
                    .UpdateAsync();
            }
        }
    }

    public async Task<ScheduledTimeout> ScheduleAsync(
        ulong guildId, ulong targetId, ulong modId, DateTime when, int durationMinutes, string reason)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<ScheduledTimeout>()
            .InsertWithOutputAsync(() => new ScheduledTimeout
            {
                GuildId = guildId,
                TargetUserId = targetId,
                ModeratorUserId = modId,
                ScheduledFor = when,
                DurationMinutes = durationMinutes,
                Reason = reason,
                Executed = false,
            });
    }

    public async Task<bool> CancelAsync(ulong guildId, int id)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<ScheduledTimeout>()
            .Where(x => x.Id == id && x.GuildId == guildId && !x.Executed)
            .DeleteAsync();
        return deleted > 0;
    }

    public async Task<List<ScheduledTimeout>> ListAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ScheduledTimeout>()
            .Where(x => x.GuildId == guildId && !x.Executed)
            .OrderBy(x => x.ScheduledFor)
            .ToListAsyncLinqToDB();
    }
}
