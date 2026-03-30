#nullable disable
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutoDeleteService : IExecOnMessage, IReadyExecutor, INService
{
    // Run after automod and auto-responder
    public int Priority => int.MaxValue - 4;

    private readonly DbService _db;

    // Cache: ChannelId → list of rules for that channel
    private readonly ConcurrentDictionary<ulong, List<AutoDeleteRule>> _channelRules = new();

    private static readonly Regex _urlRegex = new(
        @"https?://[^\s<>""']+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AutoDeleteService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var allRules = await uow.Set<AutoDeleteRule>()
            .AsNoTracking()
            .Where(r => r.Enabled)
            .ToListAsyncEF();

        foreach (var group in allRules.GroupBy(r => r.ChannelId))
            _channelRules[group.Key] = group.ToList();

        Log.Information("AutoDelete loaded {Count} rules across {ChannelCount} channels",
            allRules.Count, (object)_channelRules.Count);
    }

#pragma warning disable CS1998 // Async method lacks 'await' — fire-and-forget via Task.Run
    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null)
            return false;

        if (!_channelRules.TryGetValue(msg.Channel.Id, out var rules) || rules.Count == 0)
            return false;

        foreach (var rule in rules)
        {
            if (!ShouldDelete(rule, msg))
                continue;

            // Schedule deletion after delay
            _ = Task.Run(async () =>
            {
                try
                {
                    if (rule.DelaySeconds > 0)
                        await Task.Delay(rule.DelaySeconds * 1000);

                    await msg.DeleteAsync();
                }
                catch { }
            });

            return false; // Don't block further processing
        }

        return false;
    }
#pragma warning restore CS1998

    private static bool ShouldDelete(AutoDeleteRule rule, IUserMessage msg)
    {
        // Never delete pinned messages if configured
        if (rule.IgnorePinned && msg.IsPinned)
            return false;

        // If no filter, delete everything
        if (!rule.UseFilter || string.IsNullOrEmpty(rule.Filter))
            return true;

        var filter = rule.Filter.ToLowerInvariant();

        if (filter == "bots")
            return msg.Author.IsBot;

        if (filter == "humans")
            return !msg.Author.IsBot;

        if (filter == "attachments")
            return msg.Attachments.Count > 0;

        if (filter == "links")
            return _urlRegex.IsMatch(msg.Content ?? "");

        if (filter.StartsWith("contains:"))
        {
            var text = filter["contains:".Length..];
            return (msg.Content ?? "").Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    // ── Public API ──

    public async Task<AutoDeleteRule> AddRuleAsync(ulong guildId, ulong channelId,
        int delaySeconds = 5, string filter = null, bool ignorePinned = true)
    {
        await using var uow = _db.GetDbContext();
        var rule = new AutoDeleteRule
        {
            GuildId = guildId,
            ChannelId = channelId,
            DelaySeconds = delaySeconds,
            UseFilter = !string.IsNullOrEmpty(filter),
            Filter = filter,
            IgnorePinned = ignorePinned,
            Enabled = true,
        };

        uow.Set<AutoDeleteRule>().Add(rule);
        await uow.SaveChangesAsync();

        var channelRules = _channelRules.GetOrAdd(channelId, _ => new());
        channelRules.Add(rule);

        return rule;
    }

    public async Task<bool> RemoveRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutoDeleteRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        uow.Set<AutoDeleteRule>().Remove(rule);
        await uow.SaveChangesAsync();

        if (_channelRules.TryGetValue(rule.ChannelId, out var channelRules))
            channelRules.RemoveAll(r => r.Id == ruleId);

        return true;
    }

    public async Task<bool> ToggleRuleAsync(ulong guildId, int ruleId)
    {
        await using var uow = _db.GetDbContext();
        var rule = await uow.Set<AutoDeleteRule>()
            .FirstOrDefaultAsyncEF(r => r.Id == ruleId && r.GuildId == guildId);

        if (rule is null)
            return false;

        rule.Enabled = !rule.Enabled;
        await uow.SaveChangesAsync();

        if (_channelRules.TryGetValue(rule.ChannelId, out var channelRules))
        {
            channelRules.RemoveAll(r => r.Id == ruleId);
            if (rule.Enabled)
                channelRules.Add(rule);
        }

        return rule.Enabled;
    }

    public async Task<List<AutoDeleteRule>> GetRulesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<AutoDeleteRule>()
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .ToListAsyncEF();
    }
}
