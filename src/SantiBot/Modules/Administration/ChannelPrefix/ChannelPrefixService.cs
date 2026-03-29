#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ChannelPrefixService : INService
{
    private readonly DbService _db;
    private readonly ConcurrentDictionary<ulong, string> _prefixes = new(); // channelId -> prefix

    public ChannelPrefixService(DbService db)
    {
        _db = db;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var all = await ctx.GetTable<ChannelPrefix>().ToListAsyncLinqToDB();
            foreach (var p in all)
                _prefixes[p.ChannelId] = p.Prefix;
        }
        catch { }
    }

    public string GetPrefix(ulong channelId)
        => _prefixes.TryGetValue(channelId, out var prefix) ? prefix : null;

    public async Task SetPrefixAsync(ulong guildId, ulong channelId, string prefix)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<ChannelPrefix>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.ChannelId == channelId);

        if (existing is not null)
        {
            await ctx.GetTable<ChannelPrefix>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new ChannelPrefix { Prefix = prefix });
        }
        else
        {
            await ctx.GetTable<ChannelPrefix>()
                .InsertAsync(() => new ChannelPrefix
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    Prefix = prefix
                });
        }

        _prefixes[channelId] = prefix;
    }

    public async Task ResetPrefixAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<ChannelPrefix>()
            .Where(x => x.GuildId == guildId && x.ChannelId == channelId)
            .DeleteAsync();

        _prefixes.TryRemove(channelId, out _);
    }

    public async Task<List<ChannelPrefix>> ListPrefixesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ChannelPrefix>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
