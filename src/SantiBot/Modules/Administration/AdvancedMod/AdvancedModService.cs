#nullable disable
using System.Text.RegularExpressions;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Administration.Services;

public sealed class AdvancedModService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    // --- Anti-Nuke ---
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _antiNukeEnabled = new();

    // Key: guildId -> userId -> list of timestamps
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> _channelDeletes = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> _roleDeletes = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> _banActions = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> _kickActions = new();

    // --- Nickname Filter ---
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _nickFilterEnabled = new();

    // --- Mod Stats ---
    // guildId -> modId -> action counts
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, ModActionCounts>> _modStats = new();

    // --- Message Velocity ---
    // guildId -> userId -> list of message timestamps
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> _messageVelocity = new();

    private const int NUKE_CHANNEL_THRESHOLD = 3;
    private const int NUKE_ROLE_THRESHOLD = 3;
    private const int NUKE_BAN_THRESHOLD = 5;
    private const int NUKE_WINDOW_SECONDS = 60;
    private const int SPAM_MESSAGE_THRESHOLD = 10;
    private const int SPAM_WINDOW_SECONDS = 10;

    public AdvancedModService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.ChannelDestroyed += OnChannelDestroyed;
        _client.RoleDeleted += OnRoleDeleted;
        _client.UserBanned += OnUserBanned;
        _client.UserLeft += OnUserLeft;
        _client.GuildMemberUpdated += OnGuildMemberUpdated;
        _client.MessageReceived += OnMessageReceived;

        return Task.CompletedTask;
    }

    // ===================== ANTI-NUKE =====================

    public bool ToggleAntiNuke(ulong guildId)
    {
        var enabled = _antiNukeEnabled.AddOrUpdate(guildId, true, (_, old) => !old);
        if (enabled)
        {
            _channelDeletes.TryAdd(guildId, new());
            _roleDeletes.TryAdd(guildId, new());
            _banActions.TryAdd(guildId, new());
            _kickActions.TryAdd(guildId, new());
        }
        return enabled;
    }

    public bool IsAntiNukeEnabled(ulong guildId)
        => _antiNukeEnabled.TryGetValue(guildId, out var v) && v;

    private async Task OnChannelDestroyed(SocketChannel channel)
    {
        if (channel is not SocketGuildChannel guildChannel)
            return;

        var guild = guildChannel.Guild;
        if (!IsAntiNukeEnabled(guild.Id))
            return;

        var logs = await guild.GetAuditLogsAsync(1, actionType: ActionType.ChannelDeleted).FlattenAsync();
        var entry = logs.FirstOrDefault();
        if (entry is null)
            return;

        var userId = entry.User.Id;
        await CheckNukeThreshold(guild, userId, _channelDeletes, NUKE_CHANNEL_THRESHOLD, "channel deletions");
    }

    private async Task OnRoleDeleted(SocketRole role)
    {
        var guild = role.Guild;
        if (!IsAntiNukeEnabled(guild.Id))
            return;

        var logs = await guild.GetAuditLogsAsync(1, actionType: ActionType.RoleDeleted).FlattenAsync();
        var entry = logs.FirstOrDefault();
        if (entry is null)
            return;

        var userId = entry.User.Id;
        await CheckNukeThreshold(guild, userId, _roleDeletes, NUKE_ROLE_THRESHOLD, "role deletions");
    }

    private async Task OnUserBanned(SocketUser user, SocketGuild guild)
    {
        if (!IsAntiNukeEnabled(guild.Id))
            return;

        var logs = await guild.GetAuditLogsAsync(1, actionType: ActionType.Ban).FlattenAsync();
        var entry = logs.FirstOrDefault();
        if (entry is null)
            return;

        var bannerUserId = entry.User.Id;
        await CheckNukeThreshold(guild, bannerUserId, _banActions, NUKE_BAN_THRESHOLD, "bans");
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        if (!IsAntiNukeEnabled(guild.Id))
            return;

        var logs = await guild.GetAuditLogsAsync(1, actionType: ActionType.Kick).FlattenAsync();
        var entry = logs.FirstOrDefault();
        if (entry is null)
            return;

        // Only process if the user was actually kicked (not a voluntary leave)
        if (entry.CreatedAt > DateTimeOffset.UtcNow.AddSeconds(-5))
        {
            var kickerId = entry.User.Id;
            await CheckNukeThreshold(guild, kickerId, _kickActions, NUKE_BAN_THRESHOLD, "kicks");
        }
    }

    private async Task CheckNukeThreshold(
        SocketGuild guild,
        ulong userId,
        System.Collections.Concurrent.ConcurrentDictionary<ulong, System.Collections.Concurrent.ConcurrentDictionary<ulong, List<DateTime>>> tracker,
        int threshold,
        string actionName)
    {
        var guildTracker = tracker.GetOrAdd(guild.Id, _ => new());
        var timestamps = guildTracker.GetOrAdd(userId, _ => new());

        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-NUKE_WINDOW_SECONDS);

        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < cutoff);
            timestamps.Add(now);

            if (timestamps.Count < threshold)
                return;

            // Reset so we don't keep firing
            timestamps.Clear();
        }

        // Nuke detected! Strip the user's roles
        try
        {
            var nuker = guild.GetUser(userId);
            if (nuker is null || nuker.Id == guild.OwnerId)
                return;

            // Remove all roles with permissions
            var dangerousRoles = nuker.Roles
                .Where(r => !r.IsEveryone && (r.Permissions.Administrator || r.Permissions.ManageChannels
                    || r.Permissions.ManageRoles || r.Permissions.BanMembers || r.Permissions.KickMembers))
                .ToList();

            foreach (var role in dangerousRoles)
            {
                await nuker.RemoveRoleAsync(role);
            }

            // Alert in the first available text channel
            var alertChannel = guild.TextChannels
                .OrderBy(c => c.Position)
                .FirstOrDefault(c => guild.CurrentUser.GetPermissions(c).SendMessages);

            if (alertChannel is not null)
            {
                await alertChannel.SendMessageAsync(
                    $"**ANTI-NUKE TRIGGERED**\n" +
                    $"User {nuker.Mention} (`{nuker.Id}`) performed {threshold}+ {actionName} in {NUKE_WINDOW_SECONDS}s.\n" +
                    $"Their dangerous permissions have been stripped.");
            }
        }
        catch
        {
            // Best effort - bot may lack permissions
        }
    }

    // ===================== USER RISK SCORE =====================

    public RiskAssessment CalculateRiskScore(IGuildUser user)
    {
        var breakdown = new List<string>();
        var score = 0;

        // Account age
        var accountAge = DateTimeOffset.UtcNow - user.CreatedAt;
        if (accountAge.TotalDays < 1)
        {
            score += 40;
            breakdown.Add($"Account age: {accountAge.TotalHours:F1}h old (+40)");
        }
        else if (accountAge.TotalDays < 7)
        {
            score += 30;
            breakdown.Add($"Account age: {accountAge.TotalDays:F1} days (+30)");
        }
        else if (accountAge.TotalDays < 30)
        {
            score += 15;
            breakdown.Add($"Account age: {accountAge.TotalDays:F0} days (+15)");
        }
        else
        {
            breakdown.Add($"Account age: {accountAge.TotalDays:F0} days (+0)");
        }

        // Avatar
        if (user.GetAvatarUrl() is null)
        {
            score += 15;
            breakdown.Add("No avatar (+15)");
        }
        else
        {
            breakdown.Add("Has avatar (+0)");
        }

        // Bio check (activities can hint at bio)
        if (user is SocketGuildUser sgu && sgu.Activities.Count == 0)
        {
            score += 10;
            breakdown.Add("No status/bio (+10)");
        }
        else
        {
            breakdown.Add("Has status/bio (+0)");
        }

        // Username pattern analysis
        if (HasRandomUsername(user.Username))
        {
            score += 20;
            breakdown.Add($"Suspicious username pattern (+20)");
        }
        else
        {
            breakdown.Add("Normal username (+0)");
        }

        // Mutual guilds (only available for socket users)
        if (user is SocketGuildUser socketUser)
        {
            var mutualCount = socketUser.MutualGuilds.Count;
            if (mutualCount <= 1)
            {
                score += 15;
                breakdown.Add($"Mutual servers: {mutualCount} (+15)");
            }
            else if (mutualCount <= 3)
            {
                score += 5;
                breakdown.Add($"Mutual servers: {mutualCount} (+5)");
            }
            else
            {
                breakdown.Add($"Mutual servers: {mutualCount} (+0)");
            }
        }

        score = Math.Min(score, 100);

        var level = score switch
        {
            >= 70 => "HIGH",
            >= 40 => "MEDIUM",
            _ => "LOW"
        };

        return new RiskAssessment
        {
            Score = score,
            Level = level,
            Breakdown = breakdown,
            UserId = user.Id,
            Username = user.ToString()
        };
    }

    private static bool HasRandomUsername(string username)
    {
        // Check for excessive numbers
        var digitCount = username.Count(char.IsDigit);
        if (digitCount > username.Length / 2 && username.Length > 4)
            return true;

        // Check for lack of vowels (common in random strings)
        var lower = username.ToLowerInvariant();
        var vowelCount = lower.Count(c => "aeiou".Contains(c));
        if (vowelCount == 0 && username.Length > 5)
            return true;

        // Check for excessive special characters
        var specialCount = username.Count(c => !char.IsLetterOrDigit(c) && c != '_' && c != '.');
        if (specialCount > username.Length / 3 && username.Length > 4)
            return true;

        return false;
    }

    // ===================== NICKNAME FILTER =====================

    public bool ToggleNickFilter(ulong guildId)
        => _nickFilterEnabled.AddOrUpdate(guildId, true, (_, old) => !old);

    public bool IsNickFilterEnabled(ulong guildId)
        => _nickFilterEnabled.TryGetValue(guildId, out var v) && v;

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        if (!IsNickFilterEnabled(after.Guild.Id))
            return;

        var displayName = after.Nickname ?? after.Username;
        var violation = CheckNicknameViolation(after.Guild, displayName);

        if (violation is not null)
        {
            try
            {
                await after.ModifyAsync(u => u.Nickname = "Moderated Nickname");
            }
            catch
            {
                // Bot may lack permissions
            }
        }
    }

    public string CheckNicknameViolation(SocketGuild guild, string name)
    {
        // Check for hoisting characters
        if (IsHoisted(name))
            return "Hoisted character detected (symbol at start to appear at top of member list)";

        // Check for zalgo text
        if (IsZalgo(name))
            return "Zalgo text detected (stacked combining characters)";

        // Check for impersonation of admins
        var admins = guild.Users
            .Where(u => u.GuildPermissions.Administrator && !u.IsBot)
            .ToList();

        foreach (var admin in admins)
        {
            var adminName = admin.Nickname ?? admin.Username;
            if (adminName.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue; // It IS the admin

            var distance = LevenshteinDistance(name.ToLowerInvariant(), adminName.ToLowerInvariant());
            if (distance > 0 && distance <= 2 && name.Length >= 3)
                return $"Possible impersonation of admin '{adminName}' (similarity distance: {distance})";
        }

        return null;
    }

    private static bool IsHoisted(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var first = name[0];
        // Characters used to hoist to the top of the member list
        return first is '!' or '"' or '#' or '$' or '%' or '&' or '\'' or '(' or ')' or '*'
            or '+' or ',' or '-' or '.' or '/' or ':' or ';' or '<' or '=' or '>'
            or '?' or '@' or '[' or '\\' or ']' or '^' or '`' or '{' or '|' or '}' or '~';
    }

    private static bool IsZalgo(string name)
    {
        // Zalgo text has many combining diacritical marks (Unicode category Mn)
        var combiningCount = name.Count(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                == System.Globalization.UnicodeCategory.NonSpacingMark);

        return combiningCount > name.Length / 3 && combiningCount >= 3;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var matrix = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) matrix[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) matrix[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }

    // ===================== MOD STATS =====================

    public void RecordModAction(ulong guildId, ulong modId, ModActionType actionType)
    {
        var guildStats = _modStats.GetOrAdd(guildId, _ => new());
        var counts = guildStats.GetOrAdd(modId, _ => new ModActionCounts());

        switch (actionType)
        {
            case ModActionType.Warn: Interlocked.Increment(ref counts.Warns); break;
            case ModActionType.Mute: Interlocked.Increment(ref counts.Mutes); break;
            case ModActionType.Kick: Interlocked.Increment(ref counts.Kicks); break;
            case ModActionType.Ban: Interlocked.Increment(ref counts.Bans); break;
            case ModActionType.MessageDelete: Interlocked.Increment(ref counts.MessageDeletes); break;
        }
    }

    public List<(ulong ModId, ModActionCounts Counts)> GetModStats(ulong guildId)
    {
        if (!_modStats.TryGetValue(guildId, out var guildStats))
            return new();

        return guildStats
            .Select(kvp => (kvp.Key, kvp.Value))
            .OrderByDescending(x => x.Value.Total)
            .ToList();
    }

    // ===================== MESSAGE VELOCITY =====================

    private Task OnMessageReceived(SocketMessage msg)
    {
        if (msg.Author.IsBot || msg is not SocketUserMessage userMsg)
            return Task.CompletedTask;

        if (userMsg.Channel is not SocketTextChannel textChannel)
            return Task.CompletedTask;

        var guildId = textChannel.Guild.Id;
        var userId = msg.Author.Id;

        var guildTracker = _messageVelocity.GetOrAdd(guildId, _ => new());
        var timestamps = guildTracker.GetOrAdd(userId, _ => new());

        var now = DateTime.UtcNow;

        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < now.AddSeconds(-SPAM_WINDOW_SECONDS));
            timestamps.Add(now);
        }

        return Task.CompletedTask;
    }

    public List<(ulong UserId, int MessageCount)> GetFlaggedSpammers(ulong guildId)
    {
        if (!_messageVelocity.TryGetValue(guildId, out var guildTracker))
            return new();

        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-SPAM_WINDOW_SECONDS);
        var flagged = new List<(ulong, int)>();

        foreach (var (userId, timestamps) in guildTracker)
        {
            int count;
            lock (timestamps)
            {
                timestamps.RemoveAll(t => t < cutoff);
                count = timestamps.Count;
            }

            if (count >= SPAM_MESSAGE_THRESHOLD)
                flagged.Add((userId, count));
        }

        return flagged.OrderByDescending(x => x.Item2).ToList();
    }
}

// ===================== SUPPORTING TYPES =====================

public class RiskAssessment
{
    public int Score { get; set; }
    public string Level { get; set; }
    public List<string> Breakdown { get; set; } = new();
    public ulong UserId { get; set; }
    public string Username { get; set; }
}

public class ModActionCounts
{
    public int Warns;
    public int Mutes;
    public int Kicks;
    public int Bans;
    public int MessageDeletes;
    public int Total => Warns + Mutes + Kicks + Bans + MessageDeletes;
}

public enum ModActionType
{
    Warn,
    Mute,
    Kick,
    Ban,
    MessageDelete
}
