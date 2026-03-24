#nullable disable
using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutomodService : IExecOnMessage, IReadyExecutor, INService
{
    // Runs before normal command processing but after permission checks
    public int Priority => int.MaxValue - 2;

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    // In-memory cache: GuildId → list of active rules
    private readonly ConcurrentDictionary<ulong, List<AutomodRule>> _rules = new();

    // Rate tracking: GuildId → UserId → FilterType → list of timestamps
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, ConcurrentDictionary<AutomodFilterType, List<DateTime>>>> _rateTracker = new();

    // Duplicate message tracking: GuildId → UserId → last message content
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, (string Content, int Count, DateTime FirstSent)>> _dupeTracker = new();

    // Known phishing domains (basic list — can be expanded)
    private static readonly HashSet<string> _phishingDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord-nitro.gift", "discordgift.site", "discord-app.com", "discordapp.gift",
        "steamcommunity.net", "steampowered.net", "steamcommunitiy.com",
        "free-nitro.com", "discord-airdrop.com", "discordnitro.gift",
    };

    // Zalgo regex — detects combining diacritical marks abuse
    private static readonly Regex _zalgoRegex = new(@"[\u0300-\u036f\u0489]{3,}", RegexOptions.Compiled);

    // URL regex
    private static readonly Regex _urlRegex = new(
        @"https?://[^\s<>""']+|www\.[^\s<>""']+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Masked link regex — [text](url)
    private static readonly Regex _maskedLinkRegex = new(
        @"\[.+?\]\(https?://[^\s)]+\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Discord invite regex
    private static readonly Regex _inviteRegex = new(
        @"(?:discord\.gg|discord\.com/invite|discordapp\.com/invite)/[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AutomodService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var allRules = await uow.Set<AutomodRule>()
            .AsNoTracking()
            .Include(r => r.Exemptions)
            .Where(r => r.Enabled)
            .ToListAsyncEF();

        foreach (var group in allRules.GroupBy(r => r.GuildId))
            _rules[group.Key] = group.ToList();

        Log.Information("Automod loaded {RuleCount} rules across {GuildCount} guilds",
            allRules.Count, (object)_rules.Count);
    }

    /// <summary>
    /// Called for every message. Returns true if the message should be blocked.
    /// </summary>
    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser user)
            return false;

        // Skip bots and admins
        if (user.IsBot || user.GuildPermissions.Administrator || user.GuildPermissions.ManageMessages)
            return false;

        if (!_rules.TryGetValue(guild.Id, out var rules) || rules.Count == 0)
            return false;

        foreach (var rule in rules)
        {
            // Check exemptions
            if (IsExempt(rule, user, msg.Channel.Id))
                continue;

            var triggered = rule.FilterType switch
            {
                AutomodFilterType.BannedWords => CheckBannedWords(rule, msg.Content),
                AutomodFilterType.MassCaps => CheckMassCaps(rule, msg.Content),
                AutomodFilterType.DuplicateText => CheckDuplicateText(rule, guild.Id, user.Id, msg.Content),
                AutomodFilterType.FastSpam => CheckRateLimit(rule, guild.Id, user.Id),
                AutomodFilterType.MassMentions => CheckMassMentions(rule, msg),
                AutomodFilterType.EmojiSpam => CheckEmojiSpam(rule, msg.Content),
                AutomodFilterType.NewlineSpam => CheckNewlineSpam(rule, msg.Content),
                AutomodFilterType.AttachmentSpam => CheckRateLimit(rule, guild.Id, user.Id, msg.Attachments.Count > 0),
                AutomodFilterType.SpoilerAbuse => CheckSpoilerAbuse(rule, msg.Content),
                AutomodFilterType.ZalgoText => CheckZalgo(msg.Content),
                AutomodFilterType.PhishingLinks => CheckPhishingLinks(msg.Content),
                AutomodFilterType.MaskedLinks => CheckMaskedLinks(msg.Content),
                AutomodFilterType.InviteLinks => CheckInviteLinks(msg.Content),
                AutomodFilterType.AllLinks => CheckAllLinks(msg.Content),
                AutomodFilterType.LinkBlacklist => CheckLinkBlacklist(rule, msg.Content),
                AutomodFilterType.LinkWhitelist => CheckLinkWhitelist(rule, msg.Content),
                AutomodFilterType.StickerSpam => msg.Stickers.Count > rule.Threshold,
                AutomodFilterType.ExternalEmoji => CheckExternalEmoji(rule, msg.Content, guild.Id),
                AutomodFilterType.SelfbotDetection => false, // TODO: implement
                _ => false,
            };

            if (triggered)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteActionAsync(rule, guild, user, msg);
                        await RecordInfractionAsync(rule, guild.Id, user.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Automod action failed for rule {RuleId}", rule.Id);
                    }
                });
                return true; // Block further processing
            }
        }

        return false;
    }

    // ── Filter Checks ──

    private bool CheckBannedWords(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(rule.PatternOrList) || string.IsNullOrEmpty(content))
            return false;

        var patterns = rule.PatternOrList.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pattern in patterns)
        {
            try
            {
                if (pattern.Contains('*') || pattern.Contains('?'))
                {
                    // Wildcard → convert to regex
                    var regexPattern = "\\b" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "\\b";
                    if (Regex.IsMatch(content, regexPattern, RegexOptions.IgnoreCase))
                        return true;
                }
                else if (pattern.StartsWith('/') && pattern.EndsWith('/'))
                {
                    // Explicit regex
                    if (Regex.IsMatch(content, pattern[1..^1], RegexOptions.IgnoreCase))
                        return true;
                }
                else
                {
                    // Simple word match
                    if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // Invalid regex — skip
            }
        }

        return false;
    }

    private static bool CheckMassCaps(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length < 8)
            return false;

        var letters = content.Where(char.IsLetter).ToList();
        if (letters.Count < 5)
            return false;

        var capsPercent = (double)letters.Count(char.IsUpper) / letters.Count * 100;
        return capsPercent >= rule.Threshold;
    }

    private bool CheckDuplicateText(AutomodRule rule, ulong guildId, ulong userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var guildTracker = _dupeTracker.GetOrAdd(guildId, _ => new());
        var normalized = content.Trim().ToLowerInvariant();

        if (guildTracker.TryGetValue(userId, out var last))
        {
            if (last.Content == normalized && (DateTime.UtcNow - last.FirstSent).TotalSeconds <= rule.TimeWindowSeconds)
            {
                var newCount = last.Count + 1;
                guildTracker[userId] = (normalized, newCount, last.FirstSent);
                return newCount >= rule.Threshold;
            }
        }

        guildTracker[userId] = (normalized, 1, DateTime.UtcNow);
        return false;
    }

    private bool CheckRateLimit(AutomodRule rule, ulong guildId, ulong userId, bool condition = true)
    {
        if (!condition)
            return false;

        var guildRates = _rateTracker.GetOrAdd(guildId, _ => new());
        var userRates = guildRates.GetOrAdd(userId, _ => new());
        var timestamps = userRates.GetOrAdd(rule.FilterType, _ => new());

        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-rule.TimeWindowSeconds);

        lock (timestamps)
        {
            timestamps.RemoveAll(t => t < cutoff);
            timestamps.Add(now);
            return timestamps.Count >= rule.Threshold;
        }
    }

    private static bool CheckMassMentions(AutomodRule rule, IUserMessage msg)
    {
        var mentionCount = msg.MentionedUserIds.Count + msg.MentionedRoleIds.Count;
        if (msg.MentionedEveryone)
            mentionCount += 10; // Weight @everyone heavily
        return mentionCount >= rule.Threshold;
    }

    private static bool CheckEmojiSpam(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        // Count both unicode emojis and custom Discord emojis
        var customEmojiCount = Regex.Matches(content, @"<a?:\w+:\d+>").Count;
        var unicodeEmojiCount = Regex.Matches(content, @"[\u{1F600}-\u{1F64F}\u{1F300}-\u{1F5FF}\u{1F680}-\u{1F6FF}\u{1F1E0}-\u{1F1FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]").Count;

        return (customEmojiCount + unicodeEmojiCount) >= rule.Threshold;
    }

    private static bool CheckNewlineSpam(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        return content.Count(c => c == '\n') >= rule.Threshold;
    }

    private static bool CheckSpoilerAbuse(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        return Regex.Matches(content, @"\|\|").Count >= rule.Threshold;
    }

    private static bool CheckZalgo(string content)
        => !string.IsNullOrEmpty(content) && _zalgoRegex.IsMatch(content);

    private static bool CheckPhishingLinks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var urls = _urlRegex.Matches(content);
        foreach (Match url in urls)
        {
            if (Uri.TryCreate(url.Value, UriKind.Absolute, out var uri))
            {
                if (_phishingDomains.Contains(uri.Host))
                    return true;
            }
        }

        return false;
    }

    private static bool CheckMaskedLinks(string content)
        => !string.IsNullOrEmpty(content) && _maskedLinkRegex.IsMatch(content);

    private static bool CheckInviteLinks(string content)
        => !string.IsNullOrEmpty(content) && _inviteRegex.IsMatch(content);

    private static bool CheckAllLinks(string content)
        => !string.IsNullOrEmpty(content) && _urlRegex.IsMatch(content);

    private static bool CheckLinkBlacklist(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(rule.PatternOrList))
            return false;

        var blockedDomains = rule.PatternOrList.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var urls = _urlRegex.Matches(content);

        foreach (Match url in urls)
        {
            if (Uri.TryCreate(url.Value, UriKind.Absolute, out var uri))
            {
                if (blockedDomains.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }

        return false;
    }

    private static bool CheckLinkWhitelist(AutomodRule rule, string content)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(rule.PatternOrList))
            return false;

        // If message has no links, it's fine
        if (!_urlRegex.IsMatch(content))
            return false;

        var allowedDomains = rule.PatternOrList.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var urls = _urlRegex.Matches(content);

        foreach (Match url in urls)
        {
            if (Uri.TryCreate(url.Value, UriKind.Absolute, out var uri))
            {
                if (!allowedDomains.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
                    return true; // Found a link NOT in whitelist
            }
        }

        return false;
    }

    private static bool CheckExternalEmoji(AutomodRule rule, string content, ulong guildId)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        // Custom emoji format: <:name:id> or <a:name:id>
        var matches = Regex.Matches(content, @"<a?:\w+:(\d+)>");
        return matches.Count >= rule.Threshold;
    }

    // ── Exemption Check ──

    private static bool IsExempt(AutomodRule rule, IGuildUser user, ulong channelId)
    {
        if (rule.Exemptions is null || rule.Exemptions.Count == 0)
            return false;

        foreach (var ex in rule.Exemptions)
        {
            if (ex.Type == AutomodExemptionType.Channel && ex.ExemptId == channelId)
                return true;
            if (ex.Type == AutomodExemptionType.Role && user.RoleIds.Contains(ex.ExemptId))
                return true;
        }

        return false;
    }

    // ── Action Execution ──

    private async Task ExecuteActionAsync(AutomodRule rule, IGuild guild, IGuildUser user, IUserMessage msg)
    {
        // Always try to delete the offending message
        try { await msg.DeleteAsync(); }
        catch { }

        switch (rule.Action)
        {
            case AutomodAction.Delete:
                // Already deleted above
                break;

            case AutomodAction.Warn:
                Log.Information("Automod: Warned {User} in {Guild} for {Filter}", user, guild.Name, rule.FilterType);
                break;

            case AutomodAction.Mute:
                try
                {
                    var muteRole = guild.Roles.FirstOrDefault(r => r.Name.Equals("Muted", StringComparison.OrdinalIgnoreCase));
                    if (muteRole is not null)
                        await user.AddRoleAsync(muteRole);
                }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to mute user"); }
                break;

            case AutomodAction.TimeOut:
                try
                {
                    var duration = rule.ActionDurationMinutes > 0
                        ? TimeSpan.FromMinutes(rule.ActionDurationMinutes)
                        : TimeSpan.FromMinutes(5);
                    await user.SetTimeOutAsync(duration);
                }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to timeout user"); }
                break;

            case AutomodAction.TempMute:
                try
                {
                    var duration = rule.ActionDurationMinutes > 0
                        ? TimeSpan.FromMinutes(rule.ActionDurationMinutes)
                        : TimeSpan.FromMinutes(10);
                    await user.SetTimeOutAsync(duration);
                }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to temp-mute user"); }
                break;

            case AutomodAction.Kick:
                try { await user.KickAsync($"Automod: {rule.FilterType}"); }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to kick user"); }
                break;

            case AutomodAction.Ban:
                try { await guild.AddBanAsync(user, reason: $"Automod: {rule.FilterType}"); }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to ban user"); }
                break;

            case AutomodAction.TempBan:
                try { await guild.AddBanAsync(user, reason: $"Automod: {rule.FilterType} (temp {rule.ActionDurationMinutes}m)"); }
                catch (Exception ex) { Log.Warning(ex, "Automod: Failed to temp-ban user"); }
                break;

            case AutomodAction.CustomResponse:
                if (!string.IsNullOrEmpty(rule.CustomResponseText))
                {
                    try
                    {
                        var response = rule.CustomResponseText
                            .Replace("{user}", user.Mention)
                            .Replace("{server}", guild.Name)
                            .Replace("{channel}", $"<#{msg.Channel.Id}>");
                        await msg.Channel.SendMessageAsync(response);
                    }
                    catch { }
                }
                break;
        }
    }

    // ── Infraction Tracking ──

    private async Task RecordInfractionAsync(AutomodRule rule, ulong guildId, ulong userId)
    {
        try
        {
            await using var uow = _db.GetDbContext();
            uow.Set<AutomodInfraction>().Add(new()
            {
                GuildId = guildId,
                UserId = userId,
                FilterType = rule.FilterType,
                TriggeredAt = DateTime.UtcNow,
            });
            await uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to record automod infraction");
        }
    }

    // ── Public API for Commands ──

    public async Task<AutomodRule> AddRuleAsync(ulong guildId, AutomodFilterType filterType, AutomodAction action, int threshold = 5)
    {
        await using var uow = _db.GetDbContext();
        var rule = new AutomodRule
        {
            GuildId = guildId,
            FilterType = filterType,
            Action = action,
            Threshold = threshold,
            Enabled = true,
        };

        uow.Set<AutomodRule>().Add(rule);
        await uow.SaveChangesAsync();

        // Update cache
        var guildRules = _rules.GetOrAdd(guildId, _ => new());
        guildRules.Add(rule);

        return rule;
    }

    public async Task<bool> RemoveRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutomodRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        uow.Set<AutomodRule>().Remove(rule);
        await uow.SaveChangesAsync();

        // Update cache
        if (_rules.TryGetValue(guildId, out var guildRules))
            guildRules.RemoveAll(r => r.Id == ruleId);

        return true;
    }

    public async Task<bool> ToggleRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutomodRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        rule.Enabled = !rule.Enabled;
        await uow.SaveChangesAsync();

        // Update cache
        if (_rules.TryGetValue(guildId, out var guildRules))
        {
            var cached = guildRules.FirstOrDefault(r => r.Id == ruleId);
            if (cached is not null)
                cached.Enabled = rule.Enabled;

            if (!rule.Enabled)
                guildRules.RemoveAll(r => r.Id == ruleId);
        }

        if (rule.Enabled)
        {
            var guildRulesList = _rules.GetOrAdd(guildId, _ => new());
            if (!guildRulesList.Any(r => r.Id == ruleId))
            {
                rule.Exemptions = await uow.Set<AutomodRuleExemption>()
                    .Where(e => e.AutomodRuleId == ruleId)
                    .ToListAsyncEF();
                guildRulesList.Add(rule);
            }
        }

        return rule.Enabled;
    }

    public async Task<AutomodRule> UpdateRuleAsync(ulong guildId, int ruleId,
        int? threshold = null, int? timeWindow = null, AutomodAction? action = null,
        int? actionDuration = null, string pattern = null, string customResponse = null)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutomodRule>()
            .Include(r => r.Exemptions)
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return null;

        if (threshold.HasValue) rule.Threshold = threshold.Value;
        if (timeWindow.HasValue) rule.TimeWindowSeconds = timeWindow.Value;
        if (action.HasValue) rule.Action = action.Value;
        if (actionDuration.HasValue) rule.ActionDurationMinutes = actionDuration.Value;
        if (pattern is not null) rule.PatternOrList = pattern;
        if (customResponse is not null) rule.CustomResponseText = customResponse;

        await uow.SaveChangesAsync();

        // Update cache
        if (_rules.TryGetValue(guildId, out var guildRules))
        {
            guildRules.RemoveAll(r => r.Id == ruleId);
            if (rule.Enabled)
                guildRules.Add(rule);
        }

        return rule;
    }

    public async Task<bool> AddExemptionAsync(ulong guildId, int ruleId, AutomodExemptionType type, ulong exemptId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutomodRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        uow.Set<AutomodRuleExemption>().Add(new()
        {
            AutomodRuleId = ruleId,
            Type = type,
            ExemptId = exemptId,
        });
        await uow.SaveChangesAsync();

        // Update cache
        if (_rules.TryGetValue(guildId, out var guildRules))
        {
            var cached = guildRules.FirstOrDefault(r => r.Id == ruleId);
            cached?.Exemptions.Add(new() { AutomodRuleId = ruleId, Type = type, ExemptId = exemptId });
        }

        return true;
    }

    public List<AutomodRule> GetRules(ulong guildId)
    {
        if (_rules.TryGetValue(guildId, out var rules))
            return rules.ToList();
        return new();
    }

    public async Task<List<AutomodRule>> GetAllRulesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<AutomodRule>()
            .AsNoTracking()
            .Include(r => r.Exemptions)
            .Where(r => r.GuildId == guildId)
            .ToListAsyncEF();
    }

    public async Task<int> GetInfractionCountAsync(ulong guildId, ulong userId, TimeSpan? window = null)
    {
        await using var uow = _db.GetDbContext();
        var query = uow.Set<AutomodInfraction>()
            .Where(i => i.GuildId == guildId && i.UserId == userId);

        if (window.HasValue)
        {
            var cutoff = DateTime.UtcNow - window.Value;
            query = query.Where(i => i.TriggeredAt >= cutoff);
        }

        return await query.CountAsyncEF();
    }

    public async Task ClearInfractionsAsync(ulong guildId, ulong userId)
    {
        await using var uow = _db.GetDbContext();
        await uow.Set<AutomodInfraction>()
            .Where(i => i.GuildId == guildId && i.UserId == userId)
            .DeleteAsync();
    }
}
