#nullable disable
using SantiBot.Modules.Gambling.StockMarket;
using SantiBot.Modules.Gambling.Common;
using SantiBot.Modules.Gambling.Services;
using System.Text;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Group("stock")]
    [Name("StockMarket")]
    public partial class StockMarketCommands : GamblingModule<StockMarketService>
    {
        private readonly StockMarketService sms;

        public StockMarketCommands(
            StockMarketService sms,
            GamblingConfigService gcs)
            : base(gcs)
        {
            this.sms = sms;
        }

        [Cmd]
        public async Task Stocks()
        {
            var stocks = await sms.GetAllStocksAsync();
            if (stocks.Count == 0)
            {
                await Response().Error(strs.stock_none).SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine($"{"Symbol",-8} {"Price",10} {"Change",10}");
            sb.AppendLine(new string('-', 32));

            foreach (var s in stocks)
            {
                var price = s.PriceInCents / 100.0;
                var prev = s.PreviousPriceInCents > 0 ? s.PreviousPriceInCents / 100.0 : price;
                var change = prev > 0 ? ((price - prev) / prev) * 100 : 0;
                var changeStr = change >= 0 ? $"+{change:F1}%" : $"{change:F1}%";
                var arrow = change >= 0 ? "▲" : "▼";

                sb.AppendLine($"{s.Symbol,-8} ${price,9:F2} {arrow}{changeStr,8}");
            }

            sb.AppendLine("```");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Stock Market")
                .WithDescription(sb.ToString())
                .WithFooter("Prices update every 10 minutes");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StockBuy(string symbol, int quantity = 1)
        {
            if (quantity < 1)
            {
                await Response().Error(strs.stock_invalid_quantity).SendAsync();
                return;
            }

            var (success, error, shares) = await sms.BuyStockAsync(ctx.User.Id, symbol, quantity);

            if (!success)
            {
                var errMsg = error switch
                {
                    "stock_not_found" => strs.stock_not_found,
                    "not_enough" => strs.stock_not_enough,
                    _ => strs.stock_not_found,
                };
                await Response().Error(errMsg).SendAsync();
                return;
            }

            var stock = await sms.GetStockAsync(symbol);
            var totalCost = (stock.PriceInCents * quantity) / 100;
            await Response().Confirm(strs.stock_bought(
                quantity.ToString(),
                symbol.ToUpper(),
                totalCost.ToString() + CurrencySign)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StockSell(string symbol, int quantity = 1)
        {
            if (quantity < 1)
            {
                await Response().Error(strs.stock_invalid_quantity).SendAsync();
                return;
            }

            var (success, error, revenue) = await sms.SellStockAsync(ctx.User.Id, symbol, quantity);

            if (!success)
            {
                var errMsg = error switch
                {
                    "stock_not_found" => strs.stock_not_found,
                    "not_enough_shares" => strs.stock_not_enough_shares,
                    _ => strs.stock_not_found,
                };
                await Response().Error(errMsg).SendAsync();
                return;
            }

            await Response().Confirm(strs.stock_sold(
                quantity.ToString(),
                symbol.ToUpper(),
                revenue.ToString() + CurrencySign)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task StockPortfolio()
        {
            var portfolio = await sms.GetPortfolioAsync(ctx.User.Id);

            if (portfolio.Count == 0)
            {
                await Response().Error(strs.stock_no_holdings).SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine($"{"Symbol",-8} {"Shares",6} {"Avg Cost",10} {"Current",10} {"P/L",10}");
            sb.AppendLine(new string('-', 50));

            long totalPL = 0;
            foreach (var (holding, stock) in portfolio)
            {
                var avgCost = holding.PurchasePriceInCents / 100.0;
                var current = stock.PriceInCents / 100.0;
                var pl = ((stock.PriceInCents - holding.PurchasePriceInCents) * holding.Shares) / 100;
                totalPL += pl;
                var plStr = pl >= 0 ? $"+{pl}" : $"{pl}";

                sb.AppendLine($"{stock.Symbol,-8} {holding.Shares,6} ${avgCost,9:F2} ${current,9:F2} {plStr,9}{CurrencySign}");
            }

            sb.AppendLine(new string('-', 50));
            var totalStr = totalPL >= 0 ? $"+{totalPL}" : $"{totalPL}";
            sb.AppendLine($"{"Total P/L:",36} {totalStr,9}{CurrencySign}");
            sb.AppendLine("```");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ctx.User.Username}'s Portfolio")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        public async Task StockInfo(string symbol)
        {
            var stock = await sms.GetStockAsync(symbol);
            if (stock is null)
            {
                await Response().Error(strs.stock_not_found).SendAsync();
                return;
            }

            var price = stock.PriceInCents / 100.0;
            var prev = stock.PreviousPriceInCents > 0 ? stock.PreviousPriceInCents / 100.0 : price;
            var change = prev > 0 ? ((price - prev) / prev) * 100 : 0;
            var changeStr = change >= 0 ? $"+{change:F1}%" : $"{change:F1}%";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{stock.Symbol} - {stock.CompanyName}")
                .AddField("Current Price", $"${price:F2}", true)
                .AddField("Previous Price", $"${prev:F2}", true)
                .AddField("Change", changeStr, true)
                .AddField("Last Updated", stock.LastUpdated.ToString("g"), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
