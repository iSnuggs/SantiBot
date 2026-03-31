#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class NsfwRpService : INService
{
    private readonly DbService _db;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _cache = new();
    private bool _loaded;

    public NsfwRpService(DbService db)
    {
        _db = db;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<NsfwRpConfig>()
            .Where(x => x.Enabled)
            .ToListAsyncLinqToDB();
        foreach (var c in configs)
            _cache[c.GuildId] = true;
        _loaded = true;
    }

    public async Task<bool> IsEnabledAsync(ulong guildId)
    {
        await EnsureLoadedAsync();
        return _cache.TryGetValue(guildId, out var enabled) && enabled;
    }

    public async Task SetEnabledAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<NsfwRpConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<NsfwRpConfig>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new NsfwRpConfig { Enabled = enabled });
        }
        else
        {
            ctx.Add(new NsfwRpConfig { GuildId = guildId, Enabled = enabled });
            await ctx.SaveChangesAsync();
        }

        if (enabled)
            _cache[guildId] = true;
        else
            _cache.TryRemove(guildId, out _);
    }
}
