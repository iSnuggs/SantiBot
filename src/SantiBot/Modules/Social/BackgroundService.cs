#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Social;

public sealed class BackgroundShopService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    private static readonly List<(string Id, string Name, string Hex, long Price)> _defaultBackgrounds = new()
    {
        ("sunset", "Sunset", "#FF6B35", 500),
        ("ocean", "Ocean", "#006994", 500),
        ("forest", "Forest", "#228B22", 500),
        ("galaxy", "Galaxy", "#2E0854", 750),
        ("cherry", "Cherry Blossom", "#FFB7C5", 750),
        ("midnight", "Midnight", "#191970", 500),
        ("lavender", "Lavender", "#E6E6FA", 500),
        ("crimson", "Crimson", "#DC143C", 750),
        ("emerald", "Emerald", "#50C878", 750),
        ("gold", "Gold", "#FFD700", 1000),
        ("rose", "Rose Gold", "#B76E79", 1000),
        ("arctic", "Arctic", "#E0FFFF", 500),
        ("volcanic", "Volcanic", "#8B0000", 1000),
        ("neon", "Neon", "#39FF14", 1500),
        ("diamond", "Diamond", "#B9F2FF", 2000),
    };

    public BackgroundShopService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public List<(string Id, string Name, string Hex, long Price)> GetShopItems()
        => _defaultBackgrounds;

    public (string Id, string Name, string Hex, long Price)? GetBackground(string id)
        => _defaultBackgrounds.FirstOrDefault(x => x.Id == id.ToLowerInvariant());

    public async Task<bool> OwnsBackgroundAsync(ulong userId, string bgId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<OwnedBackground>()
            .AnyAsyncLinqToDB(x => x.UserId == userId && x.BackgroundId == bgId);
    }

    public async Task<List<OwnedBackground>> GetOwnedAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<OwnedBackground>()
            .Where(x => x.UserId == userId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> BuyBackgroundAsync(ulong userId, string bgId, long price)
    {
        var removed = await _cs.RemoveAsync(userId, price, new TxData("bgshop", "Background Purchase"));
        if (!removed)
            return false;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<OwnedBackground>()
            .InsertAsync(() => new OwnedBackground
            {
                UserId = userId,
                BackgroundId = bgId
            });
        return true;
    }
}
