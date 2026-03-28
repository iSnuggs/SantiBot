using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class StickyMessageService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // channelId -> sticky config
    private readonly NonBlocking.ConcurrentDictionary<ulong, Db.Models.StickyMessage> _stickies = new();
    // channelId -> message count since last re-stick
    private readonly NonBlocking.ConcurrentDictionary<ulong, int> _messageCounts = new();

    private const int REPOST_THRESHOLD = 5;

    public StickyMessageService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var allStickies = await ctx.GetTable<Db.Models.StickyMessage>()
            .Where(x => x.Enabled)
            .ToListAsyncLinqToDB();

        foreach (var s in allStickies)
            _stickies[s.ChannelId] = s;

        _client.MessageReceived += OnMessageReceived;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (msg.Author.IsBot)
                    return;

                if (msg.Channel is not ITextChannel textChannel)
                    return;

                var channelId = textChannel.Id;

                if (!_stickies.TryGetValue(channelId, out var sticky) || !sticky.Enabled)
                    return;

                // Increment message count
                var count = _messageCounts.AddOrUpdate(channelId, 1, (_, c) => c + 1);

                if (count < REPOST_THRESHOLD)
                    return;

                // Reset counter
                _messageCounts[channelId] = 0;

                // Delete old sticky message
                if (sticky.CurrentMessageId.HasValue)
                {
                    try
                    {
                        var oldMsg = await textChannel.GetMessageAsync(sticky.CurrentMessageId.Value);
                        if (oldMsg is not null)
                            await oldMsg.DeleteAsync();
                    }
                    catch
                    {
                        // Message may already be deleted
                    }
                }

                // Post new sticky
                var newMsg = await textChannel.SendMessageAsync($"**[Sticky]** {sticky.Content}");
                sticky.CurrentMessageId = newMsg.Id;

                // Update DB
                await using var dbCtx = _db.GetDbContext();
                await dbCtx.GetTable<Db.Models.StickyMessage>()
                    .Where(x => x.ChannelId == channelId && x.GuildId == sticky.GuildId)
                    .Set(x => x.CurrentMessageId, newMsg.Id)
                    .UpdateAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error in sticky message handler");
            }
        });

        return Task.CompletedTask;
    }

    public async Task SetStickyAsync(ulong guildId, ulong channelId, string content)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<Db.Models.StickyMessage>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId);

        if (existing is null)
        {
            var newSticky = new Db.Models.StickyMessage
            {
                GuildId = guildId,
                ChannelId = channelId,
                Content = content,
                Enabled = true
            };
            await ctx.GetTable<Db.Models.StickyMessage>().InsertAsync(() => new Db.Models.StickyMessage
            {
                GuildId = guildId,
                ChannelId = channelId,
                Content = content,
                Enabled = true
            });
            _stickies[channelId] = newSticky;
        }
        else
        {
            await ctx.GetTable<Db.Models.StickyMessage>()
                .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
                .Set(x => x.Content, content)
                .Set(x => x.Enabled, true)
                .Set(x => x.CurrentMessageId, (ulong?)null)
                .UpdateAsync();

            existing.Content = content;
            existing.Enabled = true;
            existing.CurrentMessageId = null;
            _stickies[channelId] = existing;
        }

        _messageCounts[channelId] = 0;
    }

    public async Task<bool> RemoveStickyAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<Db.Models.StickyMessage>()
            .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
            .DeleteAsync();

        _stickies.TryRemove(channelId, out _);
        _messageCounts.TryRemove(channelId, out _);

        return deleted > 0;
    }

    public async Task<List<Db.Models.StickyMessage>> GetAllStickiesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Db.Models.StickyMessage>()
            .Where(x => x.GuildId == guildId && x.Enabled)
            .ToListAsyncLinqToDB();
    }

    public Db.Models.StickyMessage? GetSticky(ulong channelId)
        => _stickies.TryGetValue(channelId, out var sticky) ? sticky : null;
}
