#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.CryptoSim;

public sealed class CryptoService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private Timer _priceTimer;
    private static readonly SantiRandom _rng = new();

    public static readonly Dictionary<string, long> DefaultCoins = new()
    {
        ["SantiCoin"] = 1000,
        ["DogeBone"] = 50,
        ["MoonToken"] = 500,
        ["DiamondPaw"] = 2500,
        ["RocketFuel"] = 150,
    };

    public CryptoService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public Task OnReadyAsync()
    {
        _priceTimer = new Timer(async _ => await UpdatePrices(), null, TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(60));
        return Task.CompletedTask;
    }

    private async Task UpdatePrices()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var allCoins = await ctx.GetTable<CryptoCoin>().ToListAsyncLinqToDB();

            // Group by guild to update per-guild prices
            foreach (var coin in allCoins)
            {
                var change = (_rng.NextDouble() * 0.30) - 0.15; // ±15%
                var newPrice = Math.Max(1, (long)(coin.Price * (1 + change)));

                await ctx.GetTable<CryptoCoin>()
                    .Where(c => c.Id == coin.Id)
                    .UpdateAsync(c => new CryptoCoin { Price = newPrice, LastUpdated = DateTime.UtcNow });
            }
        }
        catch { /* timer safety */ }
    }

    private async Task EnsureCoinsExist(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        foreach (var (name, price) in DefaultCoins)
        {
            var exists = await ctx.GetTable<CryptoCoin>()
                .AnyAsyncLinqToDB(c => c.GuildId == guildId && c.Name == name);

            if (!exists)
            {
                await ctx.GetTable<CryptoCoin>().InsertAsync(() => new CryptoCoin
                {
                    GuildId = guildId,
                    Name = name,
                    Price = price,
                    LastUpdated = DateTime.UtcNow,
                    DateAdded = DateTime.UtcNow
                });
            }
        }
    }

    public async Task<List<CryptoCoin>> ListCoinsAsync(ulong guildId)
    {
        await EnsureCoinsExist(guildId);
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CryptoCoin>()
            .Where(c => c.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Message)> BuyCryptoAsync(ulong guildId, ulong userId, string coinName, double amount)
    {
        await EnsureCoinsExist(guildId);
        if (amount <= 0)
            return (false, "Amount must be positive!");

        await using var ctx = _db.GetDbContext();
        var coin = await ctx.GetTable<CryptoCoin>()
            .FirstOrDefaultAsyncLinqToDB(c => c.GuildId == guildId && c.Name == coinName);

        if (coin is null)
            return (false, $"Unknown coin. Use `.crypto list` to see available coins.");

        var cost = (long)(coin.Price * amount);
        if (cost < 1) cost = 1;

        var removed = await _cs.RemoveAsync(userId, cost, new TxData("crypto", "buy"));
        if (!removed)
            return (false, $"You need {cost} 🥠 to buy {amount} {coinName}!");

        var holding = await ctx.GetTable<CryptoHolding>()
            .FirstOrDefaultAsyncLinqToDB(h => h.GuildId == guildId && h.UserId == userId && h.CoinName == coinName);

        if (holding is null)
        {
            await ctx.GetTable<CryptoHolding>().InsertAsync(() => new CryptoHolding
            {
                GuildId = guildId,
                UserId = userId,
                CoinName = coinName,
                Amount = amount,
                DateAdded = DateTime.UtcNow
            });
        }
        else
        {
            await ctx.GetTable<CryptoHolding>()
                .Where(h => h.Id == holding.Id)
                .UpdateAsync(h => new CryptoHolding { Amount = holding.Amount + amount });
        }

        return (true, $"Bought **{amount:F2} {coinName}** for {cost} 🥠 (price: {coin.Price} 🥠 each)!");
    }

    public async Task<(bool Success, string Message)> SellCryptoAsync(ulong guildId, ulong userId, string coinName, double amount)
    {
        if (amount <= 0)
            return (false, "Amount must be positive!");

        await using var ctx = _db.GetDbContext();
        var holding = await ctx.GetTable<CryptoHolding>()
            .FirstOrDefaultAsyncLinqToDB(h => h.GuildId == guildId && h.UserId == userId && h.CoinName == coinName);

        if (holding is null || holding.Amount < amount)
            return (false, $"You don't have enough {coinName}!");

        var coin = await ctx.GetTable<CryptoCoin>()
            .FirstOrDefaultAsyncLinqToDB(c => c.GuildId == guildId && c.Name == coinName);

        if (coin is null)
            return (false, "Coin not found!");

        var revenue = (long)(coin.Price * amount);
        if (revenue < 1) revenue = 1;

        var remaining = holding.Amount - amount;
        if (remaining < 0.001)
        {
            await ctx.GetTable<CryptoHolding>().DeleteAsync(h => h.Id == holding.Id);
        }
        else
        {
            await ctx.GetTable<CryptoHolding>()
                .Where(h => h.Id == holding.Id)
                .UpdateAsync(h => new CryptoHolding { Amount = remaining });
        }

        await _cs.AddAsync(userId, revenue, new TxData("crypto", "sell"));

        return (true, $"Sold **{amount:F2} {coinName}** for {revenue} 🥠!");
    }

    public async Task<List<CryptoHolding>> GetPortfolioAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CryptoHolding>()
            .Where(h => h.GuildId == guildId && h.UserId == userId)
            .ToListAsyncLinqToDB();
    }
}
