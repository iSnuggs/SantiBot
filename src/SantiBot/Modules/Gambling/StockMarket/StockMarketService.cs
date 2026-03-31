#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling.StockMarket;

public sealed class StockMarketService(DbService _db, ICurrencyService _cs) : INService, IReadyExecutor
{
    private static readonly SantiRandom _rng = new();
    private Timer _fluctuationTimer;

    private static readonly (string Symbol, string Name)[] _defaultStocks =
    [
        ("DSCRD", "Discord Inc."),
        ("SANTI", "SantiBot Corp."),
        ("MEMES", "Meme Factory Ltd."),
        ("GAMES", "GameStream Holdings"),
        ("MUSIC", "BeatDrop Audio"),
        ("CRYPTO", "CryptoMoon Ventures"),
        ("TWITCH", "StreamKing Media"),
        ("REDDIT", "FrontPage Networks"),
    ];

    public async Task OnReadyAsync()
    {
        await SeedStocksAsync();

        // Fluctuate prices every 10 minutes
        _fluctuationTimer = new Timer(
            async _ => await FluctuatePricesAsync(),
            null,
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10));
    }

    private async Task SeedStocksAsync()
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<Stock>().CountAsync();
        if (existing > 0)
            return;

        foreach (var (symbol, name) in _defaultStocks)
        {
            var startPrice = _rng.Next(5000, 20001); // $50.00 - $200.00
            ctx.Set<Stock>().Add(new Stock
            {
                Symbol = symbol,
                CompanyName = name,
                PriceInCents = startPrice,
                PreviousPriceInCents = startPrice,
                LastUpdated = DateTime.UtcNow,
            });
        }

        await ctx.SaveChangesAsync();
    }

    private async Task FluctuatePricesAsync()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var stocks = await ctx.GetTable<Stock>().ToListAsyncLinqToDB();

            foreach (var stock in stocks)
            {
                var changePercent = (_rng.Next(-5, 6)) / 100.0; // -5% to +5%

                // 5% chance of a big swing: -15% to +15%
                if (_rng.Next(1, 21) == 1)
                    changePercent = (_rng.Next(-15, 16)) / 100.0;

                var newPrice = (long)(stock.PriceInCents * (1.0 + changePercent));
                newPrice = Math.Max(100, newPrice); // Floor: $1.00

                await ctx.GetTable<Stock>()
                    .Where(x => x.Id == stock.Id)
                    .UpdateAsync(_ => new Stock
                    {
                        PreviousPriceInCents = stock.PriceInCents,
                        PriceInCents = newPrice,
                        LastUpdated = DateTime.UtcNow,
                    });
            }
        }
        catch
        {
            // Silently handle timer exceptions
        }
    }

    public async Task<List<Stock>> GetAllStocksAsync()
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Stock>().ToListAsyncLinqToDB();
    }

    public async Task<Stock> GetStockAsync(string symbol)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Stock>()
            .FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpper());
    }

    public async Task<(bool success, string error, int shares)> BuyStockAsync(
        ulong userId, string symbol, int quantity)
    {
        if (quantity <= 0 || quantity > 10_000)
            return (false, "invalid_quantity", 0);

        await using var ctx = _db.GetDbContext();

        var stock = await ctx.GetTable<Stock>()
            .FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpper());

        if (stock is null)
            return (false, "stock_not_found", 0);

        var totalCost = (stock.PriceInCents * quantity) / 100; // Convert cents to currency
        var taken = await _cs.RemoveAsync(userId, totalCost, new("stocks", "buy", $"Bought {quantity}x {symbol}"));
        if (!taken)
            return (false, "not_enough", 0);

        // Check if user already holds this stock
        var existing = await ctx.GetTable<StockHolding>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.StockId == stock.Id);

        if (existing is not null)
        {
            // Average the purchase price
            var totalShares = existing.Shares + quantity;
            var avgPrice = ((existing.PurchasePriceInCents * existing.Shares)
                + (stock.PriceInCents * quantity)) / totalShares;

            await ctx.GetTable<StockHolding>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new StockHolding
                {
                    Shares = totalShares,
                    PurchasePriceInCents = avgPrice,
                });
        }
        else
        {
            ctx.Set<StockHolding>().Add(new StockHolding
            {
                UserId = userId,
                StockId = stock.Id,
                Shares = quantity,
                PurchasePriceInCents = stock.PriceInCents,
            });
            await ctx.SaveChangesAsync();
        }

        return (true, null, quantity);
    }

    public async Task<(bool success, string error, long revenue)> SellStockAsync(
        ulong userId, string symbol, int quantity)
    {
        if (quantity <= 0)
            return (false, "invalid_quantity", 0);

        await using var ctx = _db.GetDbContext();

        var stock = await ctx.GetTable<Stock>()
            .FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpper());

        if (stock is null)
            return (false, "stock_not_found", 0);

        var holding = await ctx.GetTable<StockHolding>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.StockId == stock.Id);

        if (holding is null || holding.Shares < quantity)
            return (false, "not_enough_shares", 0);

        var revenue = (stock.PriceInCents * quantity) / 100;
        await _cs.AddAsync(userId, revenue, new("stocks", "sell", $"Sold {quantity}x {symbol}"));

        var remaining = holding.Shares - quantity;
        if (remaining == 0)
        {
            await ctx.GetTable<StockHolding>()
                .Where(x => x.Id == holding.Id)
                .DeleteAsync();
        }
        else
        {
            await ctx.GetTable<StockHolding>()
                .Where(x => x.Id == holding.Id)
                .UpdateAsync(_ => new StockHolding { Shares = remaining });
        }

        return (true, null, revenue);
    }

    public async Task<List<(StockHolding Holding, Stock Stock)>> GetPortfolioAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var holdings = await ctx.GetTable<StockHolding>()
            .Where(x => x.UserId == userId)
            .ToListAsyncLinqToDB();

        var result = new List<(StockHolding, Stock)>();
        foreach (var h in holdings)
        {
            var stock = await ctx.GetTable<Stock>()
                .FirstOrDefaultAsync(x => x.Id == h.StockId);
            if (stock is not null)
                result.Add((h, stock));
        }

        return result;
    }
}
