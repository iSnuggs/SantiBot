using SantiBot.Modules.Gambling.Common;
using SantiBot.Modules.Gambling.UserShop;
using SantiBot.Modules.Gambling.Services;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("UserShop")]
    [Group("usershop")]
    public partial class UserShopCommands : GamblingModule<UserShopService>
    {
        private readonly UserShopService _shop;
        private readonly DiscordSocketClient _client;

        public UserShopCommands(GamblingConfigService gcs,
            UserShopService shop,
            DiscordSocketClient client) : base(gcs)
        {
            _shop = shop;
            _client = client;
        }

        [Cmd]
        public async Task UserShop()
        {
            var listings = await _shop.GetListingsAsync(ctx.Guild.Id);

            if (listings.Count == 0)
            {
                await Response().Error(strs.shop_empty).SendAsync();
                return;
            }

            var desc = string.Join('\n', listings.Select(l =>
            {
                var seller = _client.GetUser(l.SellerUserId);
                var stock = l.Stock < 0 ? "\u221e" : l.Stock.ToString();
                return $"`#{l.Id}` **{l.ItemName}** - {N(l.Price)} (Stock: {stock})\n  {l.Description} — *{seller?.ToString() ?? "Unknown"}*";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.shop_title))
                .WithDescription(desc);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task UShopList(string itemName, long price, [Leftover] string description = "")
        {
            if (string.IsNullOrWhiteSpace(itemName) || itemName.Length > 64)
            {
                await Response().Error(strs.shop_name_invalid).SendAsync();
                return;
            }

            if (price <= 0)
            {
                await Response().Error(strs.shop_price_invalid).SendAsync();
                return;
            }

            var listing = await _shop.CreateListingAsync(ctx.Guild.Id, ctx.User.Id, itemName, price, description);

            await Response().Confirm(strs.shop_listed(listing.ItemName, N(listing.Price), listing.Id)).SendAsync();
        }

        [Cmd]
        public async Task UShopBuy(int listingId)
        {
            var (success, error, listing) = await _shop.BuyListingAsync(ctx.Guild.Id, ctx.User.Id, listingId);

            if (!success)
            {
                var msg = error switch
                {
                    "listing_not_found" => strs.shop_not_found,
                    "cant_buy_own" => strs.shop_cant_buy_own,
                    "out_of_stock" => strs.shop_out_of_stock,
                    "not_enough" => strs.not_enough(CurrencySign),
                    _ => strs.shop_buy_fail
                };
                await Response().Error(msg).SendAsync();
                return;
            }

            await Response().Confirm(strs.shop_bought(listing!.ItemName, N(listing.Price))).SendAsync();
        }

        [Cmd]
        public async Task UShopRemove(int listingId)
        {
            var success = await _shop.RemoveListingAsync(ctx.Guild.Id, ctx.User.Id, listingId);

            if (!success)
            {
                await Response().Error(strs.shop_not_found).SendAsync();
                return;
            }

            await Response().Confirm(strs.shop_removed(listingId)).SendAsync();
        }

        [Cmd]
        public async Task UShopMylistings()
        {
            var listings = await _shop.GetUserListingsAsync(ctx.Guild.Id, ctx.User.Id);

            if (listings.Count == 0)
            {
                await Response().Error(strs.shop_no_listings).SendAsync();
                return;
            }

            var desc = string.Join('\n', listings.Select(l =>
            {
                var stock = l.Stock < 0 ? "\u221e" : l.Stock.ToString();
                return $"`#{l.Id}` **{l.ItemName}** - {N(l.Price)} (Stock: {stock})";
            }));

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.shop_my_title))
                .WithDescription(desc);

            await Response().Embed(eb).SendAsync();
        }
    }
}
