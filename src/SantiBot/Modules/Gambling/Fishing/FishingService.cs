using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling.Fishing;

public sealed class FishingService(ICurrencyService _cur, DbService _db) : INService
{
    private static readonly (string Name, string Rarity, double Weight, int MinGrams, int MaxGrams, long BaseValue)[] _fishTable =
    [
        ("Sardine",     "Common",    0.30, 50,   200,   10),
        ("Bass",        "Common",    0.25, 200,  2000,  25),
        ("Trout",       "Uncommon",  0.18, 300,  3000,  50),
        ("Salmon",      "Uncommon",  0.12, 1000, 5000,  80),
        ("Tuna",        "Rare",      0.06, 5000, 30000, 200),
        ("Swordfish",   "Rare",      0.04, 10000,50000, 350),
        ("Octopus",     "Epic",      0.025,2000, 15000, 500),
        ("Shark",       "Epic",      0.015,20000,80000, 750),
        ("Golden Koi",  "Legendary", 0.007,500,  3000,  2000),
        ("Kraken",      "Legendary", 0.003,50000,200000,5000),
    ];

    private static readonly Dictionary<string, (int Cost, double CatchBonus)> _rodUpgrades = new()
    {
        ["Basic"]   = (0,    0.0),
        ["Bronze"]  = (500,  0.05),
        ["Silver"]  = (2000, 0.10),
        ["Gold"]    = (8000, 0.18),
        ["Diamond"] = (25000,0.30),
    };

    private static readonly string[] _rodOrder = ["Basic", "Bronze", "Silver", "Gold", "Diamond"];

    private static readonly TimeSpan COOLDOWN = TimeSpan.FromSeconds(30);

    private readonly Random _rng = new();

    public async Task<(FishCatch? fish, TimeSpan? cooldownLeft)> CastLineAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        // Check cooldown from most recent catch
        var lastCatch = await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CaughtAt)
            .FirstOrDefaultAsync();

        if (lastCatch is not null)
        {
            var elapsed = DateTime.UtcNow - lastCatch.CaughtAt;
            if (elapsed < COOLDOWN)
                return (null, COOLDOWN - elapsed);
        }

        // Get rod bonus
        var rod = await ctx.Set<FishingRod>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        double rodBonus = rod is not null && _rodUpgrades.TryGetValue(rod.RodType, out var info)
            ? info.CatchBonus
            : 0.0;

        // Pick a fish — rod bonus shifts rarity upward
        var roll = _rng.NextDouble();
        // Apply rod bonus: reduce roll so rarer fish (lower cumulative weight) are more likely
        roll = Math.Max(0, roll - rodBonus);

        double cumulative = 0;
        var picked = _fishTable[0];
        foreach (var f in _fishTable)
        {
            cumulative += f.Weight;
            if (roll <= cumulative)
            {
                picked = f;
                break;
            }
        }

        var weight = _rng.Next(picked.MinGrams, picked.MaxGrams + 1);
        var value = (long)(picked.BaseValue * (weight / (double)picked.MinGrams));

        var fish = new FishCatch
        {
            UserId = userId,
            FishName = picked.Name,
            Rarity = picked.Rarity,
            Weight = weight,
            SellValue = value,
            CaughtAt = DateTime.UtcNow
        };

        await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .InsertAsync(() => new FishCatch
            {
                UserId = userId,
                FishName = fish.FishName,
                Rarity = fish.Rarity,
                Weight = fish.Weight,
                SellValue = fish.SellValue,
                CaughtAt = fish.CaughtAt
            });

        return (fish, null);
    }

    public async Task<List<FishCatch>> GetBucketAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CaughtAt)
            .Take(20)
            .ToListAsyncLinqToDB();
    }

    public async Task<long> SellAllFishAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var fish = await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .ToListAsyncLinqToDB();

        if (fish.Count == 0)
            return 0;

        var total = fish.Sum(f => f.SellValue);

        await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .DeleteAsync();

        await _cur.AddAsync(userId, total, new("fishing", "sell"));
        return total;
    }

    public async Task<(string currentRod, string? nextRod, int upgradeCost)> GetRodInfoAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var rod = await ctx.Set<FishingRod>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var current = rod?.RodType ?? "Basic";
        var idx = Array.IndexOf(_rodOrder, current);
        string? next = idx < _rodOrder.Length - 1 ? _rodOrder[idx + 1] : null;
        int cost = next is not null ? _rodUpgrades[next].Cost : 0;

        return (current, next, cost);
    }

    public async Task<(bool success, string? newRod)> UpgradeRodAsync(ulong userId)
    {
        var (current, next, cost) = await GetRodInfoAsync(userId);
        if (next is null)
            return (false, null);

        if (!await _cur.RemoveAsync(userId, cost, new("fishing", "rod_upgrade")))
            return (false, null);

        await using var ctx = _db.GetDbContext();
        await ctx.Set<FishingRod>()
            .ToLinqToDBTable()
            .InsertOrUpdateAsync(() => new FishingRod
            {
                UserId = userId,
                RodType = next,
                Level = Array.IndexOf(_rodOrder, next) + 1
            },
            _ => new FishingRod
            {
                RodType = next,
                Level = Array.IndexOf(_rodOrder, next) + 1
            },
            () => new FishingRod
            {
                UserId = userId
            });

        return (true, next);
    }

    public async Task<List<FishCatch>> GetLeaderboardAsync(int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Set<FishCatch>()
            .ToLinqToDBTable()
            .OrderByDescending(x => x.Weight)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
