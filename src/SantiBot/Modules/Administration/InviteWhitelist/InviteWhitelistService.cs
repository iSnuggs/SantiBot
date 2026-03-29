#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Administration.Services;

public sealed class InviteWhitelistService : INService, IReadyExecutor, IExecOnMessage
{
    private static readonly Regex InviteRegex = new(@"discord(?:\.gg|app\.com\/invite|\.com\/invite)\/([a-zA-Z0-9\-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, HashSet<ulong>> _whitelists = new();
    private readonly ConcurrentDictionary<ulong, bool> _enabled = new();

    public int Priority => 2;

    public InviteWhitelistService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();

        var configs = await ctx.GetTable<InviteWhitelistConfig>()
            .Where(x => x.IsEnabled)
            .ToListAsyncLinqToDB();

        foreach (var c in configs)
            _enabled[c.GuildId] = true;

        var entries = await ctx.GetTable<InviteWhitelist>()
            .ToListAsyncLinqToDB();

        foreach (var g in entries.GroupBy(x => x.GuildId))
            _whitelists[g.Key] = g.Select(x => x.AllowedServerId).ToHashSet();
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser gu || gu.GuildPermissions.ManageGuild)
            return false;

        if (!_enabled.TryGetValue(guild.Id, out var enabled) || !enabled)
            return false;

        var match = InviteRegex.Match(msg.Content);
        if (!match.Success)
            return false;

        // Check if invite is from a whitelisted server
        try
        {
            var invite = await _client.GetInviteAsync(match.Groups[1].Value);
            if (invite?.GuildId is not null && _whitelists.TryGetValue(guild.Id, out var whitelist) && whitelist.Contains(invite.GuildId.Value))
                return false;
        }
        catch { /* can't resolve invite, delete it */ }

        try { await msg.DeleteAsync(); } catch { }
        return true;
    }

    public async Task<bool> AddAsync(ulong guildId, ulong serverId)
    {
        await using var ctx = _db.GetDbContext();

        var exists = await ctx.GetTable<InviteWhitelist>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.AllowedServerId == serverId);

        if (exists) return false;

        await ctx.GetTable<InviteWhitelist>()
            .InsertAsync(() => new InviteWhitelist { GuildId = guildId, AllowedServerId = serverId });

        _whitelists.AddOrUpdate(guildId, _ => new HashSet<ulong> { serverId }, (_, set) => { set.Add(serverId); return set; });
        return true;
    }

    public async Task<bool> RemoveAsync(ulong guildId, ulong serverId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<InviteWhitelist>()
            .Where(x => x.GuildId == guildId && x.AllowedServerId == serverId)
            .DeleteAsync();

        if (deleted > 0 && _whitelists.TryGetValue(guildId, out var set))
            set.Remove(serverId);

        return deleted > 0;
    }

    public async Task<List<InviteWhitelist>> ListAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<InviteWhitelist>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> ToggleAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<InviteWhitelistConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        bool newState;
        if (config is null)
        {
            await ctx.GetTable<InviteWhitelistConfig>()
                .InsertAsync(() => new InviteWhitelistConfig { GuildId = guildId, IsEnabled = true });
            newState = true;
        }
        else
        {
            newState = !config.IsEnabled;
            await ctx.GetTable<InviteWhitelistConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new InviteWhitelistConfig { IsEnabled = newState });
        }

        _enabled[guildId] = newState;
        return newState;
    }
}
