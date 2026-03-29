#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.AuctionHouse;

public sealed class AuctionService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private readonly DiscordSocketClient _client;
    private Timer _timer;

    public AuctionService(DbService db, ICurrencyService cs, DiscordSocketClient client)
    {
        _db = db;
        _cs = cs;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _timer = new Timer(async _ => await ResolveExpiredAuctions(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async Task ResolveExpiredAuctions()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var expired = await ctx.GetTable<Auction>()
                .Where(a => a.IsActive && a.EndsAt <= DateTime.UtcNow)
                .ToListAsyncLinqToDB();

            foreach (var auction in expired)
            {
                await ctx.GetTable<Auction>()
                    .Where(a => a.Id == auction.Id)
                    .UpdateAsync(a => new Auction { IsActive = false });

                if (auction.HighestBidderId != 0)
                {
                    // Transfer funds to seller
                    await _cs.AddAsync(auction.SellerId, auction.CurrentBid, new TxData("auction", "sale"));
                }
            }
        }
        catch { /* timer safety */ }
    }

    public async Task<(bool Success, string Message)> CreateAuctionAsync(ulong guildId, ulong sellerId, string sellerName, string item, long startPrice, int durationHours)
    {
        if (startPrice < 1)
            return (false, "Starting price must be at least 1 🥠!");

        if (durationHours is < 1 or > 168)
            return (false, "Duration must be between 1 and 168 hours!");

        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<Auction>().InsertWithInt32IdentityAsync(() => new Auction
        {
            GuildId = guildId,
            SellerId = sellerId,
            SellerName = sellerName,
            ItemDescription = item,
            StartPrice = startPrice,
            CurrentBid = startPrice,
            HighestBidderId = 0,
            HighestBidderName = "",
            EndsAt = DateTime.UtcNow.AddHours(durationHours),
            IsActive = true,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"Auction #{id} created! **{item}** starting at {startPrice} 🥠. Ends in {durationHours}h.");
    }

    public async Task<(bool Success, string Message)> BidAsync(ulong guildId, ulong bidderId, string bidderName, int auctionId, long amount)
    {
        await using var ctx = _db.GetDbContext();
        var auction = await ctx.GetTable<Auction>()
            .FirstOrDefaultAsyncLinqToDB(a => a.Id == auctionId && a.GuildId == guildId && a.IsActive);

        if (auction is null)
            return (false, "Auction not found or already ended!");

        if (auction.SellerId == bidderId)
            return (false, "You can't bid on your own auction!");

        if (amount <= auction.CurrentBid)
            return (false, $"Bid must be higher than current bid of {auction.CurrentBid} 🥠!");

        // Remove currency from new bidder
        var removed = await _cs.RemoveAsync(bidderId, amount, new TxData("auction", "bid"));
        if (!removed)
            return (false, $"You don't have {amount} 🥠!");

        // Refund previous bidder
        if (auction.HighestBidderId != 0)
            await _cs.AddAsync(auction.HighestBidderId, auction.CurrentBid, new TxData("auction", "outbid-refund"));

        await ctx.GetTable<Auction>()
            .Where(a => a.Id == auctionId)
            .UpdateAsync(a => new Auction
            {
                CurrentBid = amount,
                HighestBidderId = bidderId,
                HighestBidderName = bidderName
            });

        return (true, $"Bid of {amount} 🥠 placed on auction #{auctionId}!");
    }

    public async Task<List<Auction>> ListActiveAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Auction>()
            .Where(a => a.GuildId == guildId && a.IsActive)
            .OrderBy(a => a.EndsAt)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<Auction>> GetMyAuctionsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Auction>()
            .Where(a => a.GuildId == guildId && a.SellerId == userId)
            .OrderByDescending(a => a.DateAdded)
            .Take(10)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Message)> CancelAuctionAsync(ulong guildId, ulong userId, int auctionId)
    {
        await using var ctx = _db.GetDbContext();
        var auction = await ctx.GetTable<Auction>()
            .FirstOrDefaultAsyncLinqToDB(a => a.Id == auctionId && a.GuildId == guildId && a.SellerId == userId && a.IsActive);

        if (auction is null)
            return (false, "Auction not found, not yours, or already ended!");

        if (auction.HighestBidderId != 0)
            await _cs.AddAsync(auction.HighestBidderId, auction.CurrentBid, new TxData("auction", "cancelled-refund"));

        await ctx.GetTable<Auction>()
            .Where(a => a.Id == auctionId)
            .UpdateAsync(a => new Auction { IsActive = false });

        return (true, $"Auction #{auctionId} cancelled.");
    }
}
