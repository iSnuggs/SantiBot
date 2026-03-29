#nullable disable
using System.Text.RegularExpressions;
using SantiBot.Common.ModuleBehaviors;

namespace SantiBot.Modules.Administration.Security;

public sealed class SecurityService : INService
{
    private readonly DiscordSocketClient _client;

    // Honeypot channels per guild
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, HashSet<ulong>> _honeypotChannels = new();

    // Users caught by honeypot: GuildId -> set of UserIds
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, HashSet<ulong>> _caughtUsers = new();

    // Account age gate: GuildId -> minimum days
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, int> _accountAgeGate = new();

    // --- Token / Key Leak Patterns ---
    private static readonly Regex _discordTokenPattern = new(
        @"[MN][A-Za-z\d]{23,}\.[\w-]{6}\.[\w-]{27,}",
        RegexOptions.Compiled);

    private static readonly Regex _genericApiKeyPattern = new(
        @"(?:api[_-]?key|apikey|secret)[=:]\s*['""]?[\w-]{20,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _awsKeyPattern = new(
        @"AKIA[0-9A-Z]{16}",
        RegexOptions.Compiled);

    private static readonly Regex _githubTokenPattern = new(
        @"gh[pousr]_[A-Za-z0-9_]{36,}",
        RegexOptions.Compiled);

    // --- Personal Info Patterns ---
    private static readonly Regex _phonePattern = new(
        @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex _emailPattern = new(
        @"[\w.+-]+@[\w-]+\.[\w.]+",
        RegexOptions.Compiled);

    private static readonly Regex _ssnPattern = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled);

    // --- Scam Patterns ---
    private static readonly Regex _freeNitroPattern = new(
        @"free\s+nitro|nitro\s+gift|claim\s+your\s+nitro|nitro\s+for\s+free",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _fakeDiscordUrlPattern = new(
        @"https?://(?:dicsord|discrod|disocrd|discorcl|dlscord|disc0rd|d1scord|discorb)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _steamScamPattern = new(
        @"https?://(?:steamcommunlty|stearncommunity|steamcomrnunity|steancommunity|store\.steampowerd)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _selectedScamPattern = new(
        @"you(?:'ve| have) been (?:selected|chosen)|congratulations.*(?:won|winner)|claim your (?:prize|reward)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SecurityService(DiscordSocketClient client)
    {
        _client = client;
    }

    // =============================================
    // 1. Token / Key Leak Detection
    // =============================================

    public List<string> DetectLeaks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();

        var findings = new List<string>();

        if (_discordTokenPattern.IsMatch(content))
            findings.Add("Discord Token");

        if (_awsKeyPattern.IsMatch(content))
            findings.Add("AWS Access Key");

        if (_githubTokenPattern.IsMatch(content))
            findings.Add("GitHub Token");

        if (_genericApiKeyPattern.IsMatch(content))
            findings.Add("API Key / Secret");

        return findings;
    }

    // =============================================
    // 2. Personal Info Detection
    // =============================================

    public List<string> DetectPersonalInfo(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();

        var findings = new List<string>();

        if (_ssnPattern.IsMatch(content))
            findings.Add("Social Security Number");

        if (_phonePattern.IsMatch(content))
            findings.Add("Phone Number");

        if (_emailPattern.IsMatch(content))
            findings.Add("Email Address");

        return findings;
    }

    // =============================================
    // 3. Honeypot System
    // =============================================

    public void SetHoneypot(ulong guildId, ulong channelId)
    {
        var channels = _honeypotChannels.GetOrAdd(guildId, _ => new HashSet<ulong>());
        lock (channels)
            channels.Add(channelId);
    }

    public bool RemoveHoneypot(ulong guildId, ulong channelId)
    {
        if (!_honeypotChannels.TryGetValue(guildId, out var channels))
            return false;

        lock (channels)
            return channels.Remove(channelId);
    }

    public bool IsHoneypot(ulong guildId, ulong channelId)
    {
        if (!_honeypotChannels.TryGetValue(guildId, out var channels))
            return false;

        lock (channels)
            return channels.Contains(channelId);
    }

    public List<ulong> GetHoneypotChannels(ulong guildId)
    {
        if (!_honeypotChannels.TryGetValue(guildId, out var channels))
            return new List<ulong>();

        lock (channels)
            return channels.ToList();
    }

    public void RecordCaughtUser(ulong guildId, ulong userId)
    {
        var users = _caughtUsers.GetOrAdd(guildId, _ => new HashSet<ulong>());
        lock (users)
            users.Add(userId);
    }

    public List<ulong> GetCaughtUsers(ulong guildId)
    {
        if (!_caughtUsers.TryGetValue(guildId, out var users))
            return new List<ulong>();

        lock (users)
            return users.ToList();
    }

    // =============================================
    // 4. Security Audit
    // =============================================

    public List<SecurityFinding> RunSecurityAudit(SocketGuild guild)
    {
        var findings = new List<SecurityFinding>();

        // Check roles with Administrator permission
        foreach (var role in guild.Roles)
        {
            if (role.IsEveryone)
                continue;

            if (role.Permissions.Administrator)
            {
                var memberCount = guild.Users.Count(u => u.Roles.Any(r => r.Id == role.Id));
                findings.Add(new SecurityFinding
                {
                    Severity = FindingSeverity.Critical,
                    Category = "Role Permissions",
                    Description = $"Role **{role.Name}** has Administrator permission ({memberCount} members)"
                });
            }
        }

        // Check roles with dangerous permissions
        var dangerousPerms = new[]
        {
            (GuildPermission.BanMembers, "Ban Members"),
            (GuildPermission.KickMembers, "Kick Members"),
            (GuildPermission.ManageChannels, "Manage Channels"),
            (GuildPermission.ManageRoles, "Manage Roles"),
            (GuildPermission.ManageGuild, "Manage Guild")
        };

        foreach (var role in guild.Roles)
        {
            if (role.IsEveryone || role.Permissions.Administrator)
                continue;

            var dangerous = dangerousPerms
                .Where(p => role.Permissions.Has(p.Item1))
                .Select(p => p.Item2)
                .ToList();

            if (dangerous.Count > 0)
            {
                findings.Add(new SecurityFinding
                {
                    Severity = FindingSeverity.Warning,
                    Category = "Role Permissions",
                    Description = $"Role **{role.Name}** has: {string.Join(", ", dangerous)}"
                });
            }
        }

        // Check @everyone role permissions
        var everyoneRole = guild.EveryoneRole;
        if (everyoneRole.Permissions.MentionEveryone)
        {
            findings.Add(new SecurityFinding
            {
                Severity = FindingSeverity.Critical,
                Category = "@everyone Permissions",
                Description = "@everyone can mention @everyone/@here"
            });
        }

        if (everyoneRole.Permissions.CreateInstantInvite)
        {
            findings.Add(new SecurityFinding
            {
                Severity = FindingSeverity.Warning,
                Category = "@everyone Permissions",
                Description = "@everyone can create instant invites"
            });
        }

        if (everyoneRole.Permissions.AddReactions && everyoneRole.Permissions.SendMessages)
        {
            findings.Add(new SecurityFinding
            {
                Severity = FindingSeverity.Info,
                Category = "@everyone Permissions",
                Description = "@everyone can send messages and add reactions (normal but verify this is intended)"
            });
        }

        // Check bot roles with elevated permissions
        foreach (var botUser in guild.Users.Where(u => u.IsBot))
        {
            var botRoles = botUser.Roles.Where(r => !r.IsEveryone).ToList();
            foreach (var role in botRoles)
            {
                if (role.Permissions.Administrator)
                {
                    findings.Add(new SecurityFinding
                    {
                        Severity = FindingSeverity.Warning,
                        Category = "Bot Permissions",
                        Description = $"Bot **{botUser.Username}** has Administrator via role **{role.Name}**"
                    });
                }
            }
        }

        // Check channels visible to @everyone that may be sensitive
        foreach (var channel in guild.TextChannels)
        {
            var everyoneOverwrite = channel.GetPermissionOverwrite(guild.EveryoneRole);
            // If no overwrite exists, channel inherits @everyone view — check name for sensitivity
            if (everyoneOverwrite is null || everyoneOverwrite.Value.ViewChannel != PermValue.Deny)
            {
                var name = channel.Name.ToLowerInvariant();
                if (name.Contains("admin") || name.Contains("staff") || name.Contains("mod-")
                    || name.Contains("private") || name.Contains("secret") || name.Contains("log"))
                {
                    findings.Add(new SecurityFinding
                    {
                        Severity = FindingSeverity.Warning,
                        Category = "Channel Visibility",
                        Description = $"Channel **#{channel.Name}** appears sensitive but is visible to @everyone"
                    });
                }
            }
        }

        if (findings.Count == 0)
        {
            findings.Add(new SecurityFinding
            {
                Severity = FindingSeverity.Info,
                Category = "Overall",
                Description = "No significant security issues found. Server looks good!"
            });
        }

        return findings;
    }

    // =============================================
    // 5. Account Age Gate
    // =============================================

    public void SetAccountAgeGate(ulong guildId, int minimumDays)
    {
        if (minimumDays <= 0)
            _accountAgeGate.TryRemove(guildId, out _);
        else
            _accountAgeGate[guildId] = minimumDays;
    }

    public int GetAccountAgeGate(ulong guildId)
        => _accountAgeGate.TryGetValue(guildId, out var days) ? days : 0;

    public AccountAgeCheckResult CheckAccountAge(ulong guildId, IUser user)
    {
        if (!_accountAgeGate.TryGetValue(guildId, out var minimumDays) || minimumDays <= 0)
            return new AccountAgeCheckResult { Passed = true, MinimumDays = 0, AccountAgeDays = 0 };

        var accountAge = (DateTime.UtcNow - user.CreatedAt.UtcDateTime).TotalDays;

        return new AccountAgeCheckResult
        {
            Passed = accountAge >= minimumDays,
            MinimumDays = minimumDays,
            AccountAgeDays = (int)accountAge
        };
    }

    // =============================================
    // 6. Scam Detection
    // =============================================

    public ScamCheckResult DetectScam(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new ScamCheckResult { IsSuspicious = false };

        if (_freeNitroPattern.IsMatch(content))
            return new ScamCheckResult { IsSuspicious = true, Reason = "Free Nitro scam pattern detected" };

        if (_fakeDiscordUrlPattern.IsMatch(content))
            return new ScamCheckResult { IsSuspicious = true, Reason = "Fake Discord URL detected (typosquat)" };

        if (_steamScamPattern.IsMatch(content))
            return new ScamCheckResult { IsSuspicious = true, Reason = "Fake Steam URL detected (typosquat)" };

        if (_selectedScamPattern.IsMatch(content))
            return new ScamCheckResult { IsSuspicious = true, Reason = "\"You've been selected\" scam pattern detected" };

        return new ScamCheckResult { IsSuspicious = false };
    }

    // =============================================
    // Supporting Types
    // =============================================

    public class SecurityFinding
    {
        public FindingSeverity Severity { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
    }

    public enum FindingSeverity
    {
        Info,
        Warning,
        Critical
    }

    public class AccountAgeCheckResult
    {
        public bool Passed { get; set; }
        public int MinimumDays { get; set; }
        public int AccountAgeDays { get; set; }
    }

    public class ScamCheckResult
    {
        public bool IsSuspicious { get; set; }
        public string Reason { get; set; }
    }
}
