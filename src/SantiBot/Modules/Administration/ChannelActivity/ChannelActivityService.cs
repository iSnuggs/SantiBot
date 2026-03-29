#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ChannelActivityService : INService, IExecNoCommand
{
    private readonly DbService _db;
    private readonly ConcurrentDictionary<(ulong GuildId, ulong ChannelId, DateTime Date), int> _buffer = new();
    private Timer _flushTimer;

    public ChannelActivityService(DbService db)
    {
        _db = db;
        _flushTimer = new Timer(async _ => await FlushAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public Task ExecOnNoCommandAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null) return Task.CompletedTask;

        var key = (guild.Id, msg.Channel.Id, DateTime.UtcNow.Date);
        _buffer.AddOrUpdate(key, 1, (_, count) => count + 1);

        return Task.CompletedTask;
    }

    private async Task FlushAsync()
    {
        try
        {
            var items = _buffer.ToArray();
            _buffer.Clear();

            if (items.Length == 0) return;

            await using var ctx = _db.GetDbContext();

            foreach (var ((guildId, channelId, date), count) in items)
            {
                var existing = await ctx.GetTable<ChannelActivity>()
                    .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId && x.TrackedDate == date);

                if (existing is not null)
                {
                    await ctx.GetTable<ChannelActivity>()
                        .Where(x => x.Id == existing.Id)
                        .UpdateAsync(x => new ChannelActivity { MessageCount = existing.MessageCount + count });
                }
                else
                {
                    await ctx.GetTable<ChannelActivity>()
                        .InsertAsync(() => new ChannelActivity
                        {
                            GuildId = guildId,
                            ChannelId = channelId,
                            MessageCount = count,
                            TrackedDate = date
                        });
                }
            }
        }
        catch { }
    }

    public async Task<List<(ulong ChannelId, int TotalMessages)>> GetActivityAsync(ulong guildId, int days)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);

        await using var ctx = _db.GetDbContext();
        var results = await ctx.GetTable<ChannelActivity>()
            .Where(x => x.GuildId == guildId && x.TrackedDate >= cutoff)
            .GroupBy(x => x.ChannelId)
            .Select(g => new { ChannelId = g.Key, Total = g.Sum(x => x.MessageCount) })
            .OrderByDescending(x => x.Total)
            .ToListAsyncLinqToDB();

        return results.Select(r => (r.ChannelId, r.Total)).ToList();
    }
}
