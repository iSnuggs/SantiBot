using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class MessageStatsService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // Key: (guildId, userId, channelId, dateOnly) -> count to flush
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong GuildId, ulong UserId, ulong ChannelId, DateTime Date), int> _buffer = new();
    private Timer? _flushTimer;

    public MessageStatsService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        _flushTimer = new Timer(async _ => await FlushAsync(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        if (msg.Author.IsBot)
            return Task.CompletedTask;

        if (msg.Channel is not ITextChannel textChannel)
            return Task.CompletedTask;

        var key = (textChannel.GuildId, msg.Author.Id, textChannel.Id, DateTime.UtcNow.Date);
        _buffer.AddOrUpdate(key, 1, (_, count) => count + 1);

        return Task.CompletedTask;
    }

    private async Task FlushAsync()
    {
        try
        {
            if (_buffer.IsEmpty)
                return;

            // Snapshot and clear
            var snapshot = new Dictionary<(ulong, ulong, ulong, DateTime), int>();
            foreach (var kvp in _buffer)
            {
                var val = _buffer.TryRemove(kvp.Key, out var v) ? v : 0;
                if (val > 0)
                    snapshot[kvp.Key] = val;
            }

            if (snapshot.Count == 0)
                return;

            await using var ctx = _db.GetDbContext();
            foreach (var ((guildId, userId, channelId, date), count) in snapshot)
            {
                var updated = await ctx.GetTable<MessageStat>()
                    .Where(x => x.GuildId == guildId
                        && x.UserId == userId
                        && x.ChannelId == channelId
                        && x.Date == date)
                    .Set(x => x.MessageCount, x => x.MessageCount + count)
                    .UpdateAsync();

                if (updated == 0)
                {
                    await ctx.GetTable<MessageStat>()
                        .InsertAsync(() => new MessageStat
                        {
                            GuildId = guildId,
                            UserId = userId,
                            ChannelId = channelId,
                            Date = date,
                            MessageCount = count
                        });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error flushing message stats");
        }
    }

    public async Task<List<(ulong UserId, long TotalMessages)>> GetTopUsersAsync(ulong guildId, int days = 7)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<MessageStat>()
            .Where(x => x.GuildId == guildId && x.Date >= since)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.MessageCount) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.UserId, (long)x.Total)).ToList());
    }

    public async Task<List<(ulong ChannelId, long TotalMessages)>> GetTopChannelsAsync(ulong guildId, int days = 7)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<MessageStat>()
            .Where(x => x.GuildId == guildId && x.Date >= since)
            .GroupBy(x => x.ChannelId)
            .Select(g => new { ChannelId = g.Key, Total = g.Sum(x => x.MessageCount) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.ChannelId, (long)x.Total)).ToList());
    }

    public async Task<(long TotalMessages, int ActiveDays, ulong TopChannelId)?> GetUserStatsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var stats = await ctx.GetTable<MessageStat>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .ToListAsyncLinqToDB();

        if (stats.Count == 0)
            return null;

        var totalMessages = stats.Sum(x => (long)x.MessageCount);
        var activeDays = stats.Select(x => x.Date).Distinct().Count();
        var topChannel = stats
            .GroupBy(x => x.ChannelId)
            .OrderByDescending(g => g.Sum(x => x.MessageCount))
            .First().Key;

        return (totalMessages, activeDays, topChannel);
    }
}
