#nullable disable
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutobanService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    private readonly ConcurrentDictionary<ulong, List<AutobanRule>> _rules = new();

    public AutobanService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var allRules = await uow.Set<AutobanRule>()
            .AsNoTracking()
            .Where(r => r.Enabled)
            .ToListAsyncEF();

        foreach (var group in allRules.GroupBy(r => r.GuildId))
            _rules[group.Key] = group.ToList();

        _client.UserJoined += OnUserJoined;

        Log.Information("Autoban loaded {Count} rules across {GuildCount} guilds",
            allRules.Count, (object)_rules.Count);
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_rules.TryGetValue(user.Guild.Id, out var rules) || rules.Count == 0)
                    return;

                foreach (var rule in rules)
                {
                    if (!rule.Enabled)
                        continue;

                    var triggered = rule.RuleType switch
                    {
                        AutobanRuleType.AccountAge => CheckAccountAge(rule, user),
                        AutobanRuleType.Username => CheckUsername(rule, user),
                        AutobanRuleType.NoAvatar => user.GetAvatarUrl() is null,
                        _ => false,
                    };

                    if (triggered)
                    {
                        Log.Information("Autoban triggered for {User} in {Guild}: {RuleType} — {Reason}",
                            user, user.Guild.Name, rule.RuleType, rule.Reason);

                        await ExecuteActionAsync(rule, user);
                        return; // Only apply first matching rule
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Autoban check failed for {User}", user);
            }
        });

        return Task.CompletedTask;
    }

    private static bool CheckAccountAge(AutobanRule rule, SocketGuildUser user)
    {
        if (rule.MinAccountAgeHours <= 0)
            return false;

        var accountAge = DateTime.UtcNow - user.CreatedAt.UtcDateTime;
        return accountAge.TotalHours < rule.MinAccountAgeHours;
    }

    private static bool CheckUsername(AutobanRule rule, SocketGuildUser user)
    {
        if (string.IsNullOrEmpty(rule.UsernamePatterns))
            return false;

        var patterns = rule.UsernamePatterns.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var username = user.Username?.ToLowerInvariant() ?? "";
        var displayName = user.DisplayName?.ToLowerInvariant() ?? "";

        foreach (var pattern in patterns)
        {
            var lower = pattern.ToLowerInvariant();

            if (lower.Contains('*') || lower.Contains('?'))
            {
                // Wildcard match
                try
                {
                    var regexPattern = "^" + Regex.Escape(lower).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    if (Regex.IsMatch(username, regexPattern) || Regex.IsMatch(displayName, regexPattern))
                        return true;
                }
                catch { }
            }
            else
            {
                // Simple contains match
                if (username.Contains(lower) || displayName.Contains(lower))
                    return true;
            }
        }

        return false;
    }

    private async Task ExecuteActionAsync(AutobanRule rule, SocketGuildUser user)
    {
        try
        {
            switch (rule.Action)
            {
                case PunishmentAction.Ban:
                    await user.Guild.AddBanAsync(user, reason: rule.Reason);
                    break;
                case PunishmentAction.Kick:
                    await user.KickAsync(rule.Reason);
                    break;
                case PunishmentAction.TimeOut:
                    await user.SetTimeOutAsync(TimeSpan.FromHours(24), new RequestOptions { AuditLogReason = rule.Reason });
                    break;
                default:
                    await user.Guild.AddBanAsync(user, reason: rule.Reason);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Autoban failed to execute action for {User}", user);
        }
    }

    // ── Public API ──

    public async Task<AutobanRule> AddRuleAsync(ulong guildId, AutobanRuleType ruleType,
        PunishmentAction action = PunishmentAction.Ban, int minAgeHours = 0,
        string usernamePatterns = null, string reason = null)
    {
        await using var uow = _db.GetDbContext();
        var rule = new AutobanRule
        {
            GuildId = guildId,
            RuleType = ruleType,
            Action = action,
            MinAccountAgeHours = minAgeHours,
            UsernamePatterns = usernamePatterns,
            Reason = reason ?? $"Autoban: {ruleType}",
            Enabled = true,
        };

        uow.Set<AutobanRule>().Add(rule);
        await uow.SaveChangesAsync();

        var guildRules = _rules.GetOrAdd(guildId, _ => new());
        guildRules.Add(rule);

        return rule;
    }

    public async Task<bool> RemoveRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutobanRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        uow.Set<AutobanRule>().Remove(rule);
        await uow.SaveChangesAsync();

        if (_rules.TryGetValue(guildId, out var guildRules))
            guildRules.RemoveAll(r => r.Id == ruleId);

        return true;
    }

    public async Task<bool> ToggleRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutobanRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        rule.Enabled = !rule.Enabled;
        await uow.SaveChangesAsync();

        if (_rules.TryGetValue(guildId, out var guildRules))
        {
            guildRules.RemoveAll(r => r.Id == ruleId);
            if (rule.Enabled)
                guildRules.Add(rule);
        }

        return rule.Enabled;
    }

    public async Task<List<AutobanRule>> GetAllRulesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<AutobanRule>()
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .ToListAsyncEF();
    }
}
