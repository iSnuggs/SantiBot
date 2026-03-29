#nullable disable
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Administration.ServerTools;

public sealed class ServerToolsService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    // Invite Rewards: GuildId -> (UserId -> invite count)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        System.Collections.Concurrent.ConcurrentDictionary<ulong, int>> _inviteTracker = new();

    // Invite Rewards: GuildId -> (threshold -> roleId)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        Dictionary<int, ulong>> _inviteRewardRoles = new();

    private static readonly int[] InviteThresholds = [5, 10, 25, 50];

    // Emoji Stats: GuildId -> (emoji string -> count)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        System.Collections.Concurrent.ConcurrentDictionary<string, int>> _emojiStats = new();

    // Auto-Publish: GuildId -> set of channel IDs
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        HashSet<ulong>> _autoPublishChannels = new();

    // Message Mirroring: GuildId -> (sourceChannelId -> destChannelId)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        System.Collections.Concurrent.ConcurrentDictionary<ulong, ulong>> _mirrorMap = new();

    // Member Milestones: GuildId -> announcement channel ID
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, ulong> _milestoneChannels = new();

    private static readonly int[] Milestones = [100, 250, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000];

    public ServerToolsService(DiscordSocketClient client, IMessageSenderService sender)
    {
        _client = client;
        _sender = sender;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        _client.UserJoined += OnUserJoined;
        return Task.CompletedTask;
    }

    // ────────────────────────────
    //  Invite Rewards
    // ────────────────────────────

    public void SetInviteRewardRole(ulong guildId, int threshold, ulong roleId)
    {
        if (!InviteThresholds.Contains(threshold))
            return;

        var roles = _inviteRewardRoles.GetOrAdd(guildId, _ => new Dictionary<int, ulong>());
        lock (roles)
        {
            roles[threshold] = roleId;
        }
    }

    public async Task RecordInviteAsync(ulong guildId, ulong inviterId)
    {
        var guildInvites = _inviteTracker.GetOrAdd(guildId,
            _ => new System.Collections.Concurrent.ConcurrentDictionary<ulong, int>());

        var newCount = guildInvites.AddOrUpdate(inviterId, 1, (_, old) => old + 1);

        // Check if we hit a threshold and have a reward role
        if (!_inviteRewardRoles.TryGetValue(guildId, out var roles))
            return;

        ulong roleId;
        lock (roles)
        {
            if (!roles.TryGetValue(newCount, out roleId))
                return;
        }

        var guild = _client.GetGuild(guildId);
        var user = guild?.GetUser(inviterId);
        var role = guild?.GetRole(roleId);
        if (user is not null && role is not null)
        {
            try
            {
                await user.AddRoleAsync(role);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to assign invite reward role {RoleId} to user {UserId}", roleId, inviterId);
            }
        }
    }

    public int GetInviteCount(ulong guildId, ulong userId)
    {
        if (_inviteTracker.TryGetValue(guildId, out var guildInvites)
            && guildInvites.TryGetValue(userId, out var count))
            return count;

        return 0;
    }

    public Dictionary<int, ulong> GetInviteRewardRoles(ulong guildId)
    {
        if (_inviteRewardRoles.TryGetValue(guildId, out var roles))
        {
            lock (roles)
            {
                return new Dictionary<int, ulong>(roles);
            }
        }
        return new Dictionary<int, ulong>();
    }

    // ────────────────────────────
    //  Emoji Stats
    // ────────────────────────────

    public void TrackEmojis(ulong guildId, string messageContent)
    {
        var guildEmojis = _emojiStats.GetOrAdd(guildId,
            _ => new System.Collections.Concurrent.ConcurrentDictionary<string, int>());

        // Match custom Discord emojis <:name:id> and <a:name:id>
        var customMatches = System.Text.RegularExpressions.Regex.Matches(
            messageContent, @"<a?:\w+:\d+>");

        foreach (System.Text.RegularExpressions.Match m in customMatches)
            guildEmojis.AddOrUpdate(m.Value, 1, (_, old) => old + 1);

        // Match standard unicode emojis (common emoji ranges)
        var unicodeMatches = System.Text.RegularExpressions.Regex.Matches(
            messageContent, @"[\u{1F600}-\u{1F64F}\u{1F300}-\u{1F5FF}\u{1F680}-\u{1F6FF}\u{1F1E0}-\u{1F1FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]");

        foreach (System.Text.RegularExpressions.Match m in unicodeMatches)
            guildEmojis.AddOrUpdate(m.Value, 1, (_, old) => old + 1);
    }

    public List<KeyValuePair<string, int>> GetTopEmojis(ulong guildId, int count = 10)
    {
        if (!_emojiStats.TryGetValue(guildId, out var guildEmojis))
            return new List<KeyValuePair<string, int>>();

        return guildEmojis
            .OrderByDescending(x => x.Value)
            .Take(count)
            .ToList();
    }

    public void ResetEmojiStats(ulong guildId)
        => _emojiStats.TryRemove(guildId, out _);

    // ────────────────────────────
    //  Auto-Publish
    // ────────────────────────────

    public bool ToggleAutoPublish(ulong guildId, ulong channelId)
    {
        var channels = _autoPublishChannels.GetOrAdd(guildId, _ => new HashSet<ulong>());
        lock (channels)
        {
            if (!channels.Add(channelId))
            {
                channels.Remove(channelId);
                return false; // disabled
            }
            return true; // enabled
        }
    }

    public bool IsAutoPublishEnabled(ulong guildId, ulong channelId)
    {
        if (!_autoPublishChannels.TryGetValue(guildId, out var channels))
            return false;

        lock (channels)
        {
            return channels.Contains(channelId);
        }
    }

    public List<ulong> GetAutoPublishChannels(ulong guildId)
    {
        if (!_autoPublishChannels.TryGetValue(guildId, out var channels))
            return new List<ulong>();

        lock (channels)
        {
            return channels.ToList();
        }
    }

    // ────────────────────────────
    //  Message Mirroring
    // ────────────────────────────

    public void SetMirror(ulong guildId, ulong sourceChannelId, ulong destChannelId)
    {
        var guildMirrors = _mirrorMap.GetOrAdd(guildId,
            _ => new System.Collections.Concurrent.ConcurrentDictionary<ulong, ulong>());
        guildMirrors[sourceChannelId] = destChannelId;
    }

    public bool RemoveMirror(ulong guildId, ulong sourceChannelId)
    {
        if (!_mirrorMap.TryGetValue(guildId, out var guildMirrors))
            return false;

        return guildMirrors.TryRemove(sourceChannelId, out _);
    }

    public List<KeyValuePair<ulong, ulong>> GetMirrors(ulong guildId)
    {
        if (!_mirrorMap.TryGetValue(guildId, out var guildMirrors))
            return new List<KeyValuePair<ulong, ulong>>();

        return guildMirrors.ToList();
    }

    // ────────────────────────────
    //  Member Milestones
    // ────────────────────────────

    public void SetMilestoneChannel(ulong guildId, ulong channelId)
        => _milestoneChannels[guildId] = channelId;

    public void DisableMilestones(ulong guildId)
        => _milestoneChannels.TryRemove(guildId, out _);

    public ulong? GetMilestoneChannel(ulong guildId)
        => _milestoneChannels.TryGetValue(guildId, out var chId) ? chId : null;

    private async Task CheckMilestone(SocketGuild guild)
    {
        if (!_milestoneChannels.TryGetValue(guild.Id, out var channelId))
            return;

        var memberCount = guild.MemberCount;
        var milestone = Milestones.FirstOrDefault(m => m == memberCount);
        if (milestone == 0)
            return;

        var channel = guild.GetTextChannel(channelId);
        if (channel is null)
            return;

        try
        {
            var eb = _sender.CreateEmbed(guild.Id)
                .WithOkColor()
                .WithTitle("Member Milestone Reached!")
                .WithDescription($"This server just hit **{milestone:N0}** members!")
                .WithThumbnailUrl(guild.IconUrl)
                .WithFooter($"Milestone reached at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");

            await channel.SendMessageAsync(embed: eb.Build());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send milestone announcement for guild {GuildId}", guild.Id);
        }
    }

    // ────────────────────────────
    //  Server Backup Summary
    // ────────────────────────────

    public string GenerateBackupSummary(SocketGuild guild)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"# Server Backup Summary: {guild.Name}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Server ID: {guild.Id}");
        sb.AppendLine($"Owner: {guild.Owner?.Username ?? "Unknown"} ({guild.OwnerId})");
        sb.AppendLine($"Member Count: {guild.MemberCount}");
        sb.AppendLine($"Boost Level: {guild.PremiumTier} ({guild.PremiumSubscriptionCount} boosts)");
        sb.AppendLine($"Verification Level: {guild.VerificationLevel}");
        sb.AppendLine($"Content Filter: {guild.ExplicitContentFilter}");
        sb.AppendLine($"Default Notifications: {guild.DefaultMessageNotifications}");
        sb.AppendLine($"AFK Channel: {guild.AFKChannel?.Name ?? "None"} (Timeout: {guild.AFKTimeout}s)");
        sb.AppendLine($"System Channel: {guild.SystemChannel?.Name ?? "None"}");
        sb.AppendLine();

        // Roles
        sb.AppendLine("## Roles");
        foreach (var role in guild.Roles.OrderByDescending(r => r.Position))
        {
            var perms = role.Permissions.Administrator ? " [ADMIN]" : "";
            sb.AppendLine($"  - {role.Name} (ID: {role.Id}, Color: {role.Color}, Members: {role.Members.Count()}{perms})");
        }
        sb.AppendLine();

        // Categories and channels
        sb.AppendLine("## Channels");
        var categories = guild.CategoryChannels.OrderBy(c => c.Position).ToList();
        var uncategorized = guild.Channels
            .Where(c => c is not ICategoryChannel && (c as INestedChannel)?.CategoryId is null)
            .OrderBy(c => c.Position)
            .ToList();

        if (uncategorized.Any())
        {
            sb.AppendLine("  [No Category]");
            foreach (var ch in uncategorized)
                sb.AppendLine($"    - {ChannelTypePrefix(ch)}{ch.Name} (ID: {ch.Id})");
        }

        foreach (var cat in categories)
        {
            sb.AppendLine($"  [{cat.Name}]");
            var children = guild.Channels
                .OfType<INestedChannel>()
                .Where(c => c.CategoryId == cat.Id)
                .OrderBy(c => c.Position);

            foreach (var ch in children)
                sb.AppendLine($"    - {ChannelTypePrefix((IGuildChannel)ch)}{ch.Name} (ID: {((IGuildChannel)ch).Id})");
        }
        sb.AppendLine();

        // Emojis
        sb.AppendLine("## Custom Emojis");
        foreach (var emoji in guild.Emotes.OrderBy(e => e.Name))
            sb.AppendLine($"  - :{emoji.Name}: (ID: {emoji.Id}, Animated: {emoji.Animated})");
        sb.AppendLine();

        // Bans note
        sb.AppendLine("## Notes");
        sb.AppendLine("  - Ban list and audit log are not included (require separate API calls).");
        sb.AppendLine("  - This is a summary, not a restorable backup.");

        return sb.ToString();
    }

    private static string ChannelTypePrefix(IGuildChannel channel)
        => channel switch
        {
            IStageChannel => "[Stage] ",
            IVoiceChannel => "[Voice] ",
            IForumChannel => "[Forum] ",
            INewsChannel => "[News] ",
            ITextChannel => "[Text] ",
            _ => ""
        };

    // ────────────────────────────
    //  Event Handlers
    // ────────────────────────────

    private async Task OnMessageReceived(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg || msg.Author.IsBot)
            return;

        if (msg.Channel is not SocketTextChannel textChannel)
            return;

        var guildId = textChannel.Guild.Id;

        // Track emojis
        TrackEmojis(guildId, msg.Content);

        // Auto-publish for announcement channels
        if (textChannel is SocketNewsChannel newsChannel && IsAutoPublishEnabled(guildId, newsChannel.Id))
        {
            try
            {
                await userMsg.CrosspostAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to auto-publish message in channel {ChannelId}", newsChannel.Id);
            }
        }

        // Message mirroring
        if (_mirrorMap.TryGetValue(guildId, out var guildMirrors)
            && guildMirrors.TryGetValue(textChannel.Id, out var destChannelId))
        {
            var destChannel = textChannel.Guild.GetTextChannel(destChannelId);
            if (destChannel is not null)
            {
                try
                {
                    var eb = _sender.CreateEmbed(guildId)
                        .WithAuthor(msg.Author.Username, msg.Author.GetAvatarUrl())
                        .WithDescription(msg.Content)
                        .WithFooter($"Mirrored from #{textChannel.Name}")
                        .WithTimestamp(msg.Timestamp);

                    if (msg.Attachments.FirstOrDefault() is { } attachment)
                        eb.WithImageUrl(attachment.Url);

                    await destChannel.SendMessageAsync(embed: eb.Build());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to mirror message from {Source} to {Dest}", textChannel.Id, destChannelId);
                }
            }
        }
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        await CheckMilestone(user.Guild);
    }
}
