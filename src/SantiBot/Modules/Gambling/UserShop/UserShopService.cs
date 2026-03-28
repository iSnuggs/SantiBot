using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling.UserShop;

public sealed class UserShopService(ICurrencyService _cur, DbService _db) : INService
{
    public async Task<ShopListing> CreateListingAsync(ulong guildId, ulong sellerId, string itemName, long price, string description)
    {
        await using var ctx = _db.GetDbContext();

        var listing = new ShopListing
        {
            GuildId = guildId,
            SellerUserId = sellerId,
            ItemName = itemName,
            Description = description,
            Price = price,
            Stock = -1,
            Active = true
        };

        var id = await ctx.Set<ShopListing>()
            .ToLinqToDBTable()
            .InsertWithInt32IdentityAsync(() => new ShopListing
            {
                GuildId = guildId,
                SellerUserId = sellerId,
                ItemName = itemName,
                Description = description,
                Price = price,
                Stock = -1,
                Active = true
            });

        listing.Id = id;
        return listing;
    }

    public async Task<List<ShopListing>> GetListingsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Set<ShopListing>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId && x.Active)
            .OrderByDescending(x => x.DateAdded)
            .Take(25)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<ShopListing>> GetUserListingsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Set<ShopListing>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId && x.SellerUserId == userId && x.Active)
            .OrderByDescending(x => x.DateAdded)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool success, string? error, ShopListing? listing)> BuyListingAsync(ulong guildId, ulong buyerId, int listingId)
    {
        await using var ctx = _db.GetDbContext();

        var listing = await ctx.Set<ShopListing>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.Id == listingId && x.GuildId == guildId && x.Active);

        if (listing is null)
            return (false, "listing_not_found", null);

        if (listing.SellerUserId == buyerId)
            return (false, "cant_buy_own", null);

        if (listing.Stock == 0)
            return (false, "out_of_stock", null);

        // Take currency from buyer
        if (!await _cur.RemoveAsync(buyerId, listing.Price, new("usershop", "buy")))
            return (false, "not_enough", null);

        // Give currency to seller
        await _cur.AddAsync(listing.SellerUserId, listing.Price, new("usershop", "sale"));

        // Decrement stock if not unlimited
        if (listing.Stock > 0)
        {
            var newStock = listing.Stock - 1;
            await ctx.Set<ShopListing>()
                .ToLinqToDBTable()
                .Where(x => x.Id == listingId)
                .UpdateAsync(_ => new ShopListing
                {
                    Stock = newStock,
                    Active = newStock > 0
                });
        }

        // Record purchase
        await ctx.Set<ShopPurchase>()
            .ToLinqToDBTable()
            .InsertAsync(() => new ShopPurchase
            {
                ListingId = listingId,
                BuyerUserId = buyerId,
                PurchasedAt = DateTime.UtcNow
            });

        return (true, null, listing);
    }

    public async Task<bool> RemoveListingAsync(ulong guildId, ulong userId, int listingId)
    {
        await using var ctx = _db.GetDbContext();
        var rows = await ctx.Set<ShopListing>()
            .ToLinqToDBTable()
            .Where(x => x.Id == listingId && x.GuildId == guildId && x.SellerUserId == userId)
            .UpdateAsync(_ => new ShopListing { Active = false });

        return rows > 0;
    }
}
