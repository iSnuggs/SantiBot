#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ContentAgeGateService : INService, IReadyExecutor, IExecOnMessage
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, Dictionary<ulong, ulong>> _gates = new(); // guildId -> channelId -> roleId

    public int Priority => 4;

    public ContentAgeGateService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var gates = await ctx.GetTable<ContentAgeGate>()
            .ToListAsyncLinqToDB();

        foreach (var g in gates.GroupBy(x => x.GuildId))
            _gates[g.Key] = g.ToDictionary(x => x.ChannelId, x => x.RequiredRoleId);
    }

    public async Task<bool> ExecOnMessageAsync(IGuild guild, IUserMessage msg)
    {
        if (guild is null || msg.Author is not IGuildUser gu)
            return false;

        if (!_gates.TryGetValue(guild.Id, out var guildGates))
            return false;

        if (!guildGates.TryGetValue(msg.Channel.Id, out var requiredRoleId))
            return false;

        if (gu.RoleIds.Contains(requiredRoleId) || gu.GuildPermissions.Administrator)
            return false;

        try
        {
            await msg.DeleteAsync();
            var dm = await gu.CreateDMChannelAsync();
            await dm.SendMessageAsync($"You need the age-verified role to post in that channel.");
        }
        catch { }

        return true;
    }

    public async Task SetGateAsync(ulong guildId, ulong channelId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();

        // Delete existing gate for channel
        await ctx.GetTable<ContentAgeGate>()
            .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
            .DeleteAsync();

        await ctx.GetTable<ContentAgeGate>()
            .InsertAsync(() => new ContentAgeGate
            {
                GuildId = guildId,
                ChannelId = channelId,
                RequiredRoleId = roleId
            });

        _gates.AddOrUpdate(guildId,
            _ => new Dictionary<ulong, ulong> { [channelId] = roleId },
            (_, dict) => { dict[channelId] = roleId; return dict; });
    }

    public async Task<bool> RemoveGateAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<ContentAgeGate>()
            .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
            .DeleteAsync();

        if (deleted > 0 && _gates.TryGetValue(guildId, out var dict))
            dict.Remove(channelId);

        return deleted > 0;
    }

    public async Task<List<ContentAgeGate>> ListGatesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ContentAgeGate>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
