using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class InviteTrackerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IBotCreds _creds;
    private readonly IMessageSenderService _sender;
    private readonly IBotStrings _strings;
    private readonly ILocalization _localization;

    // guildId -> (inviteCode -> uses)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, Dictionary<string, int>> _inviteCache = new();

    public InviteTrackerService(
        DbService db,
        DiscordSocketClient client,
        IBotCreds creds,
        IMessageSenderService sender,
        IBotStrings strings,
        ILocalization localization)
    {
        _db = db;
        _client = client;
        _creds = creds;
        _sender = sender;
        _strings = strings;
        _localization = localization;
    }

    public async Task OnReadyAsync()
    {
        // Cache invite counts for all enabled guilds on this shard
        await using var ctx = _db.GetDbContext();

        var enabledGuilds = await ctx.GetTable<InviteTrackConfig>()
            .Where(x => x.Enabled
                         && Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToListAsyncLinqToDB();

        foreach (var config in enabledGuilds)
        {
            try
            {
                await CacheGuildInvitesAsync(config.GuildId);
            }
            catch
            {
                // Guild might not be accessible
            }
        }

        _client.UserJoined += OnUserJoined;
    }

    private async Task CacheGuildInvitesAsync(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null)
            return;

        var invites = await guild.GetInvitesAsync();
        var dict = invites.ToDictionary(x => x.Code, x => x.Uses ?? 0);
        _inviteCache[guildId] = dict;
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx.GetTable<InviteTrackConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == user.Guild.Id && x.Enabled);

        if (config is null)
            return;

        try
        {
            var newInvites = await user.Guild.GetInvitesAsync();
            var newDict = newInvites.ToDictionary(x => x.Code, x => x.Uses ?? 0);

            _inviteCache.TryGetValue(user.Guild.Id, out var oldDict);
            oldDict ??= new();

            // Find the invite whose uses incremented
            string usedCode = null;
            ulong inviterId = 0;

            foreach (var invite in newInvites)
            {
                oldDict.TryGetValue(invite.Code, out var oldUses);
                if ((invite.Uses ?? 0) > oldUses)
                {
                    usedCode = invite.Code;
                    inviterId = invite.Inviter?.Id ?? 0;
                    break;
                }
            }

            // Update cache
            _inviteCache[user.Guild.Id] = newDict;

            if (usedCode is null || inviterId == 0)
                return;

            // Log to DB
            await ctx.GetTable<TrackedInvite>()
                .InsertAsync(() => new TrackedInvite
                {
                    GuildId = user.Guild.Id,
                    InviterUserId = inviterId,
                    InvitedUserId = user.Id,
                    InviteCode = usedCode,
                    JoinedAt = DateTime.UtcNow,
                });

            // Log to channel if configured
            if (config.LogChannelId is ulong logChId)
            {
                var ch = user.Guild.GetTextChannel(logChId);
                if (ch is not null)
                {
                    var culture = _localization.GetCultureInfo(user.Guild.Id);
                    var eb = _sender.CreateEmbed(user.Guild.Id)
                        .WithOkColor()
                        .WithTitle(_strings.GetText(strs.invite_tracked, culture))
                        .WithDescription(
                            $"<@{user.Id}> joined using invite `{usedCode}` from <@{inviterId}>")
                        .WithCurrentTimestamp();

                    await _sender.Response(ch).Embed(eb).SendAsync();
                }
            }
        }
        catch
        {
            // Silently fail if we can't get invites (missing perms, etc.)
        }
    }

    public async Task<bool> EnableAsync(ulong guildId, bool enable)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx.GetTable<InviteTrackConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
        {
            await ctx.GetTable<InviteTrackConfig>()
                .InsertAsync(() => new InviteTrackConfig
                {
                    GuildId = guildId,
                    Enabled = enable,
                });
        }
        else
        {
            await ctx.GetTable<InviteTrackConfig>()
                .Where(x => x.Id == config.Id)
                .Set(x => x.Enabled, enable)
                .UpdateAsync();
        }

        if (enable)
            await CacheGuildInvitesAsync(guildId);
        else
            _inviteCache.TryRemove(guildId, out _);

        return enable;
    }

    public async Task SetLogChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx.GetTable<InviteTrackConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
        {
            await ctx.GetTable<InviteTrackConfig>()
                .InsertAsync(() => new InviteTrackConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                    LogChannelId = channelId,
                });
        }
        else
        {
            await ctx.GetTable<InviteTrackConfig>()
                .Where(x => x.Id == config.Id)
                .Set(x => x.LogChannelId, channelId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }

        await CacheGuildInvitesAsync(guildId);
    }

    public async Task<int> GetInviteCountAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<TrackedInvite>()
            .CountAsyncLinqToDB(x => x.GuildId == guildId && x.InviterUserId == userId);
    }

    public async Task<IReadOnlyList<(ulong UserId, int Count)>> GetLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();

        var results = await ctx.GetTable<TrackedInvite>()
            .Where(x => x.GuildId == guildId)
            .GroupBy(x => x.InviterUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsyncLinqToDB();

        return results.Select(x => (x.UserId, x.Count)).ToList();
    }
}
