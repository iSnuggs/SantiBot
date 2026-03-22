using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class StarboardService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly NonBlocking.ConcurrentDictionary<ulong, StarboardSettings> _settings = new();

    public StarboardService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var allSettings = await ctx.GetTable<StarboardSettings>().ToListAsyncLinqToDB();
        foreach (var s in allSettings)
            _settings[s.GuildId] = s;

        _client.ReactionAdded += OnReactionChanged;
        _client.ReactionRemoved += OnReactionChanged;
    }

    private async Task OnReactionChanged(
        Cacheable<IUserMessage, ulong> msgCache,
        Cacheable<IMessageChannel, ulong> chCache,
        SocketReaction reaction)
    {
        try
        {
            var ch = chCache.Value as ITextChannel ?? await chCache.GetOrDownloadAsync() as ITextChannel;
            if (ch is null)
                return;

            if (!_settings.TryGetValue(ch.GuildId, out var settings) || settings.StarboardChannelId == 0)
                return;

            var emote = reaction.Emote;
            if (emote.Name != settings.StarEmoji)
                return;

            var msg = await msgCache.GetOrDownloadAsync();
            if (msg is null)
                return;

            // Don't allow self-starring unless configured
            if (!settings.AllowSelfStar && reaction.UserId == msg.Author.Id)
                return;

            // Count reactions of the star emoji
            var reactionUsers = await msg.GetReactionUsersAsync(emote, 100).FlattenAsync();
            var count = settings.AllowSelfStar
                ? reactionUsers.Count(u => !u.IsBot)
                : reactionUsers.Count(u => !u.IsBot && u.Id != msg.Author.Id);

            await UpdateStarboardAsync(ch.Guild, settings, msg, count);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error in starboard reaction handler");
        }
    }

    private async Task UpdateStarboardAsync(IGuild guild, StarboardSettings settings, IUserMessage msg, int starCount)
    {
        await using var ctx = _db.GetDbContext();

        var entry = await ctx.GetTable<StarboardEntry>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guild.Id && x.MessageId == msg.Id);

        var starboardChannel = await guild.GetTextChannelAsync(settings.StarboardChannelId);
        if (starboardChannel is null)
            return;

        if (starCount < settings.StarThreshold)
        {
            // Remove from starboard if below threshold
            if (entry is not null)
            {
                try
                {
                    var sbMsg = await starboardChannel.GetMessageAsync(entry.StarboardMessageId);
                    if (sbMsg is not null)
                        await sbMsg.DeleteAsync();
                }
                catch { }

                await ctx.GetTable<StarboardEntry>().DeleteAsync(x => x.Id == entry.Id);
            }
            return;
        }

        var embed = _sender.CreateEmbed(guild.Id)
            .WithAuthor(msg.Author)
            .WithDescription(msg.Content?.TrimTo(2048) ?? "")
            .AddField("Source", $"[Jump to message]({msg.GetJumpUrl()})")
            .WithFooter($"{settings.StarEmoji} {starCount} | {msg.Channel.Name}")
            .WithTimestamp(msg.Timestamp)
            .WithOkColor();

        // Add first image attachment if any
        var img = msg.Attachments.FirstOrDefault(a =>
            a.Url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            a.Url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            a.Url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            a.Url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
            a.Url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
        if (img is not null)
            embed.WithImageUrl(img.Url);
        else if (msg.Embeds.FirstOrDefault()?.Image is { } embedImg)
            embed.WithImageUrl(embedImg.Url);

        if (entry is null)
        {
            // New starboard entry
            var sbMsg = await _sender.Response(starboardChannel)
                .Embed(embed)
                .Text($"{settings.StarEmoji} **{starCount}** | <#{msg.Channel.Id}>")
                .SendAsync();

            if (sbMsg is not null)
            {
                await ctx.GetTable<StarboardEntry>().InsertAsync(() => new StarboardEntry
                {
                    GuildId = guild.Id,
                    ChannelId = msg.Channel.Id,
                    MessageId = msg.Id,
                    AuthorId = msg.Author.Id,
                    StarboardMessageId = sbMsg.Id,
                    StarCount = starCount,
                });
            }
        }
        else
        {
            // Update existing starboard message
            try
            {
                var sbMsg = await starboardChannel.GetMessageAsync(entry.StarboardMessageId) as IUserMessage;
                if (sbMsg is not null)
                {
                    await sbMsg.ModifyAsync(x =>
                    {
                        x.Content = $"{settings.StarEmoji} **{starCount}** | <#{msg.Channel.Id}>";
                        x.Embed = embed.Build();
                    });
                }

                await ctx.GetTable<StarboardEntry>()
                    .Where(x => x.Id == entry.Id)
                    .Set(x => x.StarCount, starCount)
                    .UpdateAsync();
            }
            catch { }
        }
    }

    public async Task<bool> SetStarboardChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<StarboardSettings>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            var s = new StarboardSettings { GuildId = guildId, StarboardChannelId = channelId };
            await ctx.GetTable<StarboardSettings>().InsertAsync(() => new StarboardSettings
            {
                GuildId = guildId,
                StarboardChannelId = channelId,
                StarThreshold = 3,
                StarEmoji = "⭐",
                AllowSelfStar = false,
            });
            _settings[guildId] = s;
        }
        else
        {
            await ctx.GetTable<StarboardSettings>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.StarboardChannelId, channelId)
                .UpdateAsync();
            existing.StarboardChannelId = channelId;
            _settings[guildId] = existing;
        }

        return true;
    }

    public async Task SetThresholdAsync(ulong guildId, int threshold)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<StarboardSettings>()
            .InsertOrUpdateAsync(() => new StarboardSettings
            {
                GuildId = guildId,
                StarboardChannelId = 0,
                StarThreshold = threshold,
                StarEmoji = "⭐",
                AllowSelfStar = false,
            },
            x => new StarboardSettings { StarThreshold = threshold },
            () => new StarboardSettings { GuildId = guildId });

        if (_settings.TryGetValue(guildId, out var s))
            s.StarThreshold = threshold;
    }

    public async Task SetSelfStarAsync(ulong guildId, bool allow)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<StarboardSettings>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.AllowSelfStar, allow)
            .UpdateAsync();

        if (_settings.TryGetValue(guildId, out var s))
            s.AllowSelfStar = allow;
    }

    public StarboardSettings? GetSettings(ulong guildId)
        => _settings.TryGetValue(guildId, out var s) ? s : null;

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<StarboardSettings>().DeleteAsync(x => x.GuildId == guildId);
        _settings.TryRemove(guildId, out _);
    }
}
