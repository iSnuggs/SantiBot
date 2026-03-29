#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Administration.Services;

public sealed class RegexAutomodService : INService, IReadyExecutor, IExecOnMessage
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, List<(int Id, Regex Regex, RegexAutomodAction Action)>> _rules = new();

    public int Priority => 1;

    public RegexAutomodService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var rules = await ctx.GetTable<RegexAutomodRule>()
            .Where(x => x.IsEnabled)
            .ToListAsyncLinqToDB();

        foreach (var g in rules.GroupBy(x => x.GuildId))
        {
            var list = new List<(int, Regex, RegexAutomodAction)>();
            foreach (var r in g)
            {
                try
                {
                    list.Add((r.Id, new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)), r.Action));
                }
                catch { /* invalid regex, skip */ }
            }
            _rules[g.Key] = list;
        }
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser gu || gu.GuildPermissions.ManageGuild)
            return false;

        if (!_rules.TryGetValue(guild.Id, out var rules) || rules.Count == 0)
            return false;

        foreach (var (id, regex, action) in rules)
        {
            try
            {
                if (regex.IsMatch(msg.Content))
                {
                    switch (action)
                    {
                        case RegexAutomodAction.Delete:
                            await msg.DeleteAsync();
                            return true;
                        case RegexAutomodAction.Warn:
                            await msg.DeleteAsync();
                            return true;
                        case RegexAutomodAction.Mute:
                            await msg.DeleteAsync();
                            if (gu is SocketGuildUser sgu)
                                await sgu.SetTimeOutAsync(TimeSpan.FromMinutes(10));
                            return true;
                        case RegexAutomodAction.Ban:
                            await msg.DeleteAsync();
                            await guild.AddBanAsync(gu, reason: $"Regex automod rule #{id}");
                            return true;
                    }
                }
            }
            catch { /* timeout or error */ }
        }

        return false;
    }

    public async Task<RegexAutomodRule> AddRuleAsync(ulong guildId, string pattern, RegexAutomodAction action, ulong userId)
    {
        // Validate regex
        try { _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)); }
        catch { return null; }

        var rule = new RegexAutomodRule
        {
            GuildId = guildId,
            Pattern = pattern,
            Action = action,
            IsEnabled = true,
            AddedByUserId = userId
        };

        await using var ctx = _db.GetDbContext();
        rule.Id = await ctx.GetTable<RegexAutomodRule>()
            .InsertWithInt32IdentityAsync(() => new RegexAutomodRule
            {
                GuildId = guildId,
                Pattern = pattern,
                Action = action,
                IsEnabled = true,
                AddedByUserId = userId
            });

        var compiled = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        _rules.AddOrUpdate(guildId,
            _ => [(rule.Id, compiled, action)],
            (_, list) => { list.Add((rule.Id, compiled, action)); return list; });

        return rule;
    }

    public async Task<bool> RemoveRuleAsync(ulong guildId, int ruleId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<RegexAutomodRule>()
            .Where(x => x.GuildId == guildId && x.Id == ruleId)
            .DeleteAsync();

        if (deleted > 0 && _rules.TryGetValue(guildId, out var list))
            list.RemoveAll(x => x.Id == ruleId);

        return deleted > 0;
    }

    public async Task<List<RegexAutomodRule>> ListRulesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<RegexAutomodRule>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
