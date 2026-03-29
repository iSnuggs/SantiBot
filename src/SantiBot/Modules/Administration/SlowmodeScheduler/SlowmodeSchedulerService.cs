#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class SlowmodeSchedulerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private Timer _timer;

    public SlowmodeSchedulerService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _timer = new Timer(async _ => await CheckSchedulesAsync(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async Task CheckSchedulesAsync()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var schedules = await ctx.GetTable<SlowmodeSchedule>()
                .Where(x => x.IsEnabled)
                .ToListAsyncLinqToDB();

            var now = DateTime.UtcNow.TimeOfDay;

            foreach (var sched in schedules)
            {
                try
                {
                    var guild = _client.GetGuild(sched.GuildId);
                    if (guild is null) continue;

                    var channel = guild.GetTextChannel(sched.ChannelId);
                    if (channel is null) continue;

                    bool isActive = sched.StartTime <= sched.EndTime
                        ? now >= sched.StartTime && now < sched.EndTime
                        : now >= sched.StartTime || now < sched.EndTime;

                    var targetSlowmode = isActive ? sched.SlowmodeSeconds : 0;
                    if (channel.SlowModeInterval != targetSlowmode)
                        await channel.ModifyAsync(c => c.SlowModeInterval = targetSlowmode);
                }
                catch { /* skip individual failures */ }
            }
        }
        catch { /* swallow errors in background task */ }
    }

    public async Task<SlowmodeSchedule> AddScheduleAsync(ulong guildId, ulong channelId, int seconds, TimeSpan start, TimeSpan end)
    {
        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<SlowmodeSchedule>()
            .InsertWithInt32IdentityAsync(() => new SlowmodeSchedule
            {
                GuildId = guildId,
                ChannelId = channelId,
                SlowmodeSeconds = seconds,
                StartTime = start,
                EndTime = end,
                IsEnabled = true
            });

        return new SlowmodeSchedule { Id = id, GuildId = guildId, ChannelId = channelId, SlowmodeSeconds = seconds, StartTime = start, EndTime = end };
    }

    public async Task<bool> RemoveScheduleAsync(ulong guildId, int schedId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SlowmodeSchedule>()
            .Where(x => x.GuildId == guildId && x.Id == schedId)
            .DeleteAsync() > 0;
    }

    public async Task<List<SlowmodeSchedule>> ListSchedulesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SlowmodeSchedule>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
