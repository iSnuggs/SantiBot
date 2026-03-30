#nullable disable
using System.Text;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Modules.Games.Marketplace;

// ─────────────────────────────── SERVICE ───────────────────────────────

namespace SantiBot.Modules.Games.Marketplace
{
    public sealed class MarketplaceService(DbService _db, ICurrencyService _cs) : INService
    {
        private const int MAX_ACTIVE_LISTINGS = 10;
        private const int LISTING_DURATION_DAYS = 7;
        private const double MARKETPLACE_TAX = 0.05; // 5% tax on sales

        /// <summary>
        /// Create a new trade offer on the marketplace.
        /// </summary>
        public async Task<(TradeOffer offer, string error)> ListItemAsync(
            ulong guildDiscordId, ulong userId, string itemName, string itemType, int quantity, long pricePerUnit)
        {
            if (string.IsNullOrWhiteSpace(itemName) || itemName.Length > 50)
                return (null, "Item name must be 1-50 characters.");

            if (quantity <= 0)
                return (null, "Quantity must be at least 1.");

            if (pricePerUnit <= 0)
                return (null, "Price per unit must be at least 1.");

            itemType = NormalizeItemType(itemType);

            await using var ctx = _db.GetDbContext();

            // Check listing limit
            var activeCount = await ctx.GetTable<TradeOffer>()
                .CountAsync(x => x.SellerUserId == userId
                    && x.GuildDiscordId == guildDiscordId
                    && x.IsActive);

            if (activeCount >= MAX_ACTIVE_LISTINGS)
                return (null, $"You can only have {MAX_ACTIVE_LISTINGS} active listings at a time.");

            var offer = new TradeOffer
            {
                SellerUserId = userId,
                GuildDiscordId = guildDiscordId,
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
                PricePerUnit = pricePerUnit,
                IsActive = true,
                ListedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(LISTING_DURATION_DAYS),
            };

            ctx.Set<TradeOffer>().Add(offer);
            await ctx.SaveChangesAsync();

            return (offer, null);
        }

        /// <summary>
        /// Buy items from a marketplace listing.
        /// Transfers currency from buyer to seller (minus 5% tax).
        /// </summary>
        public async Task<(TradeTransaction transaction, string error)> BuyItemAsync(
            ulong guildDiscordId, ulong buyerId, int listingId, int quantity)
        {
            if (quantity <= 0)
                return (null, "Quantity must be at least 1.");

            await using var ctx = _db.GetDbContext();

            var offer = await ctx.GetTable<TradeOffer>()
                .FirstOrDefaultAsync(x => x.Id == listingId
                    && x.GuildDiscordId == guildDiscordId
                    && x.IsActive);

            if (offer is null)
                return (null, "Listing not found or no longer active.");

            if (offer.SellerUserId == buyerId)
                return (null, "You can't buy your own listing.");

            if (DateTime.UtcNow > offer.ExpiresAt)
            {
                // Auto-expire
                await ctx.GetTable<TradeOffer>()
                    .Where(x => x.Id == listingId)
                    .UpdateAsync(_ => new TradeOffer { IsActive = false });
                return (null, "That listing has expired.");
            }

            if (quantity > offer.Quantity)
                return (null, $"Only **{offer.Quantity}** available. You requested {quantity}.");

            var totalPrice = offer.PricePerUnit * quantity;

            // Remove currency from buyer
            var taken = await _cs.RemoveAsync(buyerId, totalPrice,
                new("marketplace", "buy", $"Bought {quantity}x {offer.ItemName}"));

            if (!taken)
                return (null, $"You need **{totalPrice:N0}** currency to buy {quantity}x {offer.ItemName}.");

            // Pay seller (minus tax)
            var tax = (long)(totalPrice * MARKETPLACE_TAX);
            var sellerPayout = totalPrice - tax;

            await _cs.AddAsync(offer.SellerUserId, sellerPayout,
                new("marketplace", "sell", $"Sold {quantity}x {offer.ItemName}"));

            // Update or deactivate listing
            var remainingQty = offer.Quantity - quantity;
            if (remainingQty <= 0)
            {
                await ctx.GetTable<TradeOffer>()
                    .Where(x => x.Id == listingId)
                    .UpdateAsync(_ => new TradeOffer { IsActive = false, Quantity = 0 });
            }
            else
            {
                await ctx.GetTable<TradeOffer>()
                    .Where(x => x.Id == listingId)
                    .UpdateAsync(_ => new TradeOffer { Quantity = remainingQty });
            }

            // Record transaction
            var transaction = new TradeTransaction
            {
                BuyerUserId = buyerId,
                SellerUserId = offer.SellerUserId,
                GuildDiscordId = guildDiscordId,
                ItemName = offer.ItemName,
                Quantity = quantity,
                TotalPrice = totalPrice,
                CompletedAt = DateTime.UtcNow,
            };

            ctx.Set<TradeTransaction>().Add(transaction);
            await ctx.SaveChangesAsync();

            return (transaction, null);
        }

        /// <summary>
        /// Cancel one of your own listings.
        /// </summary>
        public async Task<(string itemName, string error)> CancelListingAsync(
            ulong guildDiscordId, ulong userId, int listingId)
        {
            await using var ctx = _db.GetDbContext();

            var offer = await ctx.GetTable<TradeOffer>()
                .FirstOrDefaultAsync(x => x.Id == listingId
                    && x.GuildDiscordId == guildDiscordId
                    && x.IsActive);

            if (offer is null)
                return (null, "Listing not found or already inactive.");

            if (offer.SellerUserId != userId)
                return (null, "You can only cancel your own listings.");

            await ctx.GetTable<TradeOffer>()
                .Where(x => x.Id == listingId)
                .UpdateAsync(_ => new TradeOffer { IsActive = false });

            return (offer.ItemName, null);
        }

        /// <summary>
        /// Browse all active marketplace listings, optionally filtered by item type.
        /// </summary>
        public async Task<List<TradeOffer>> GetMarketplaceAsync(ulong guildDiscordId, string itemType = null)
        {
            await using var ctx = _db.GetDbContext();

            var query = ctx.GetTable<TradeOffer>()
                .Where(x => x.GuildDiscordId == guildDiscordId
                    && x.IsActive
                    && x.ExpiresAt > DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(itemType))
            {
                var normalized = NormalizeItemType(itemType);
                query = query.Where(x => x.ItemType == normalized);
            }

            return await query
                .OrderByDescending(x => x.ListedAt)
                .Take(25)
                .ToListAsyncLinqToDB();
        }

        /// <summary>
        /// Get all active listings for a specific user.
        /// </summary>
        public async Task<List<TradeOffer>> GetMyListingsAsync(ulong guildDiscordId, ulong userId)
        {
            await using var ctx = _db.GetDbContext();

            return await ctx.GetTable<TradeOffer>()
                .Where(x => x.GuildDiscordId == guildDiscordId
                    && x.SellerUserId == userId
                    && x.IsActive)
                .OrderByDescending(x => x.ListedAt)
                .ToListAsyncLinqToDB();
        }

        /// <summary>
        /// Get average price for an item over the last 7 days.
        /// </summary>
        public async Task<(string itemName, long avgPrice, int totalSold, long lowestActive, long highestActive)>
            GetPriceHistoryAsync(ulong guildDiscordId, string itemName)
        {
            await using var ctx = _db.GetDbContext();

            var cutoff = DateTime.UtcNow.AddDays(-7);
            var normalizedName = itemName.Trim().ToLowerInvariant();

            var transactions = await ctx.GetTable<TradeTransaction>()
                .Where(x => x.GuildDiscordId == guildDiscordId
                    && x.ItemName.ToLower() == normalizedName
                    && x.CompletedAt >= cutoff)
                .ToListAsyncLinqToDB();

            long avgPrice = 0;
            var totalSold = 0;

            if (transactions.Count > 0)
            {
                totalSold = transactions.Sum(x => x.Quantity);
                avgPrice = transactions.Sum(x => x.TotalPrice) / Math.Max(totalSold, 1);
            }

            // Get current active listing prices
            var activeListings = await ctx.GetTable<TradeOffer>()
                .Where(x => x.GuildDiscordId == guildDiscordId
                    && x.ItemName.ToLower() == normalizedName
                    && x.IsActive
                    && x.ExpiresAt > DateTime.UtcNow)
                .ToListAsyncLinqToDB();

            long lowestActive = 0;
            long highestActive = 0;

            if (activeListings.Count > 0)
            {
                lowestActive = activeListings.Min(x => x.PricePerUnit);
                highestActive = activeListings.Max(x => x.PricePerUnit);
            }

            return (itemName, avgPrice, totalSold, lowestActive, highestActive);
        }

        private static string NormalizeItemType(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
                return "General";

            return itemType.Trim().ToLowerInvariant() switch
            {
                "weapon" or "weapons" => "Weapon",
                "armor" or "armors" => "Armor",
                "potion" or "potions" => "Potion",
                "material" or "materials" or "mat" or "mats" => "Material",
                "food" or "foods" => "Food",
                "fish" => "Fish",
                "card" or "cards" => "Card",
                "collectible" or "collectibles" => "Collectible",
                "tool" or "tools" => "Tool",
                "misc" or "miscellaneous" => "Misc",
                _ => "General",
            };
        }
    }
}

// ─────────────────────────────── COMMANDS ───────────────────────────────

namespace SantiBot.Modules.Games
{
    public partial class Games
    {
        [Name("Marketplace")]
        [Group("market")]
        public partial class MarketplaceCommands : SantiModule<MarketplaceService>
        {
            private readonly ICurrencyProvider _cp;

            public MarketplaceCommands(ICurrencyProvider cp)
            {
                _cp = cp;
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task List(string itemName, int quantity, long pricePerUnit, [Leftover] string itemType = null)
            {
                var sign = _cp.GetCurrencySign();
                var (offer, error) = await _service.ListItemAsync(
                    ctx.Guild.Id, ctx.User.Id, itemName, itemType ?? "General", quantity, pricePerUnit);

                if (offer is null)
                {
                    await Response().Error(error).SendAsync();
                    return;
                }

                var totalValue = offer.PricePerUnit * offer.Quantity;

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle("Listing Created!")
                    .AddField("Item", offer.ItemName, true)
                    .AddField("Type", offer.ItemType, true)
                    .AddField("Quantity", offer.Quantity.ToString("N0"), true)
                    .AddField("Price/Unit", $"{offer.PricePerUnit:N0}{sign}", true)
                    .AddField("Total Value", $"{totalValue:N0}{sign}", true)
                    .AddField("Listing ID", $"#{offer.Id}", true)
                    .WithFooter($"Expires {offer.ExpiresAt:yyyy-MM-dd HH:mm} UTC | 5% tax on sales");

                await Response().Embed(eb).SendAsync();
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task Buy(int listingId, int quantity = 1)
            {
                var sign = _cp.GetCurrencySign();
                var (transaction, error) = await _service.BuyItemAsync(ctx.Guild.Id, ctx.User.Id, listingId, quantity);

                if (transaction is null)
                {
                    await Response().Error(error).SendAsync();
                    return;
                }

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle("Purchase Complete!")
                    .AddField("Item", transaction.ItemName, true)
                    .AddField("Quantity", transaction.Quantity.ToString("N0"), true)
                    .AddField("Total Paid", $"{transaction.TotalPrice:N0}{sign}", true)
                    .AddField("Seller", $"<@{transaction.SellerUserId}>", true)
                    .WithFooter("Item has been added to your inventory");

                await Response().Embed(eb).SendAsync();
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task Cancel(int listingId)
            {
                var (itemName, error) = await _service.CancelListingAsync(ctx.Guild.Id, ctx.User.Id, listingId);

                if (itemName is null)
                {
                    await Response().Error(error).SendAsync();
                    return;
                }

                await Response().Confirm($"Cancelled listing for **{itemName}**.").SendAsync();
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task Market([Leftover] string filter = null)
            {
                var sign = _cp.GetCurrencySign();
                var listings = await _service.GetMarketplaceAsync(ctx.Guild.Id, filter);

                if (listings.Count == 0)
                {
                    var msg = string.IsNullOrWhiteSpace(filter)
                        ? "The marketplace is empty. Use `.market list` to create a listing!"
                        : $"No listings found for type **{filter}**.";
                    await Response().Error(msg).SendAsync();
                    return;
                }

                var sb = new StringBuilder();
                foreach (var offer in listings)
                {
                    var totalValue = offer.PricePerUnit * offer.Quantity;
                    sb.AppendLine(
                        $"**#{offer.Id}** | {offer.ItemName} ({offer.ItemType}) | "
                        + $"{offer.Quantity}x @ {offer.PricePerUnit:N0}{sign} ea | "
                        + $"Total: {totalValue:N0}{sign} | <@{offer.SellerUserId}>");
                }

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle("Marketplace")
                    .WithDescription(sb.ToString())
                    .WithFooter($"{listings.Count} active listing(s) | Use .market buy <id> to purchase");

                await Response().Embed(eb).SendAsync();
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task MyListings()
            {
                var sign = _cp.GetCurrencySign();
                var listings = await _service.GetMyListingsAsync(ctx.Guild.Id, ctx.User.Id);

                if (listings.Count == 0)
                {
                    await Response().Error("You have no active listings.").SendAsync();
                    return;
                }

                var sb = new StringBuilder();
                foreach (var offer in listings)
                {
                    var totalValue = offer.PricePerUnit * offer.Quantity;
                    sb.AppendLine(
                        $"**#{offer.Id}** | {offer.ItemName} ({offer.ItemType}) | "
                        + $"{offer.Quantity}x @ {offer.PricePerUnit:N0}{sign} ea | "
                        + $"Expires: {offer.ExpiresAt:MM-dd HH:mm}");
                }

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle($"Your Listings ({listings.Count}/10)")
                    .WithDescription(sb.ToString())
                    .WithFooter("Use .market cancel <id> to remove a listing");

                await Response().Embed(eb).SendAsync();
            }

            [Cmd]
            [RequireContext(ContextType.Guild)]
            public async Task PriceCheck([Leftover] string itemName)
            {
                var sign = _cp.GetCurrencySign();
                var (name, avgPrice, totalSold, lowest, highest) =
                    await _service.GetPriceHistoryAsync(ctx.Guild.Id, itemName);

                var eb = CreateEmbed()
                    .WithOkColor()
                    .WithTitle($"Price Check: {name}");

                if (totalSold > 0)
                {
                    eb.AddField("7-Day Avg Price", $"{avgPrice:N0}{sign}", true);
                    eb.AddField("Total Sold (7d)", totalSold.ToString("N0"), true);
                }
                else
                {
                    eb.AddField("7-Day Sales", "No sales in the last 7 days", false);
                }

                if (lowest > 0)
                {
                    eb.AddField("Lowest Active", $"{lowest:N0}{sign}", true);
                    eb.AddField("Highest Active", $"{highest:N0}{sign}", true);
                }
                else
                {
                    eb.AddField("Active Listings", "None currently listed", false);
                }

                eb.WithFooter("Prices based on completed transactions and active listings");

                await Response().Embed(eb).SendAsync();
            }
        }
    }
}
