using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class AfkEnhancedService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly NonBlocking.ConcurrentDictionary<(ulong GuildId, ulong UserId), AfkUser> _afkUsers = new();

    public AfkEnhancedService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var all = await ctx.GetTable<AfkUser>().ToListAsyncLinqToDB();
        foreach (var a in all)
            _afkUsers[(a.GuildId, a.UserId)] = a;

        _client.MessageReceived += OnMessageReceived;
    }

    private Task OnMessageReceived(SocketMessage sm)
    {
        if (sm.Author.IsBot || sm.Author.IsWebhook)
            return Task.CompletedTask;

        if (sm is not IUserMessage uMsg || uMsg.Channel is not ITextChannel tc)
            return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                var guildId = tc.GuildId;

                // Check if the message author is AFK -- if so, remove their AFK
                if (_afkUsers.TryRemove((guildId, sm.Author.Id), out var selfAfk))
                {
                    await using var ctx = _db.GetDbContext();
                    await ctx.GetTable<AfkUser>()
                        .Where(x => x.GuildId == guildId && x.UserId == sm.Author.Id)
                        .DeleteAsync();

                    var duration = DateTime.UtcNow - selfAfk.SetAt;
                    var durationText = FormatDuration(duration);
                    var msg = await _sender.Response(tc)
                        .Confirm($"Welcome back, {sm.Author.Mention}! You were AFK for **{durationText}**.")
                        .SendAsync();

                    msg.DeleteAfter(8);
                }

                // Check if any mentioned users are AFK
                if (uMsg.MentionedUserIds.Count is 0 or > 5)
                    return;

                foreach (var mentionedId in uMsg.MentionedUserIds)
                {
                    if (mentionedId == sm.Author.Id)
                        continue;

                    if (!_afkUsers.TryGetValue((guildId, mentionedId), out var afkEntry))
                        continue;

                    var duration = DateTime.UtcNow - afkEntry.SetAt;
                    var durationText = FormatDuration(duration);
                    var afkMessage = string.IsNullOrWhiteSpace(afkEntry.Message)
                        ? "No message set."
                        : afkEntry.Message;

                    var embed = _sender.CreateEmbed(guildId)
                        .WithTitle("User is AFK")
                        .WithDescription($"<@{mentionedId}> is currently AFK.")
                        .AddField("Message", afkMessage, false)
                        .AddField("AFK Since", $"{durationText} ago", true)
                        .WithOkColor();

                    var reply = await _sender.Response(tc)
                        .Message(uMsg)
                        .Embed(embed)
                        .SendAsync();

                    reply.DeleteAfter(15);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in AfkEnhanced message handler");
            }
        });

        return Task.CompletedTask;
    }

    public async Task SetAfkAsync(ulong guildId, ulong userId, string message)
    {
        var afk = new AfkUser
        {
            GuildId = guildId,
            UserId = userId,
            Message = message,
            SetAt = DateTime.UtcNow
        };

        await using var ctx = _db.GetDbContext();

        // Remove any existing AFK for this user in this guild
        await ctx.GetTable<AfkUser>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .DeleteAsync();

        await ctx.GetTable<AfkUser>().InsertAsync(() => new AfkUser
        {
            GuildId = guildId,
            UserId = userId,
            Message = message,
            SetAt = DateTime.UtcNow
        });

        _afkUsers[(guildId, userId)] = afk;
    }

    public async Task<bool> RemoveAfkAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<AfkUser>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .DeleteAsync();

        _afkUsers.TryRemove((guildId, userId), out _);
        return deleted > 0;
    }

    public AfkUser? GetAfk(ulong guildId, ulong userId)
        => _afkUsers.TryGetValue((guildId, userId), out var afk) ? afk : null;

    public bool IsAfk(ulong guildId, ulong userId)
        => _afkUsers.ContainsKey((guildId, userId));

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m";
        return "less than a minute";
    }
}
