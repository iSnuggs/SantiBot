#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.CryptoSim;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Crypto")]
    [Group("crypto")]
    public partial class CryptoCommands : SantiModule<CryptoService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var coins = await _service.ListCoinsAsync(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithTitle("📈 Crypto Market")
                .WithDescription("Prices update every hour (±15% volatility)")
                .WithOkColor();

            foreach (var c in coins)
            {
                eb.AddField(c.Name, $"{c.Price} 🥠", true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Buy(string coin, double amount)
        {
            var (success, message) = await _service.BuyCryptoAsync(ctx.Guild.Id, ctx.User.Id, coin, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Sell(string coin, double amount)
        {
            var (success, message) = await _service.SellCryptoAsync(ctx.Guild.Id, ctx.User.Id, coin, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Portfolio()
        {
            var holdings = await _service.GetPortfolioAsync(ctx.Guild.Id, ctx.User.Id);
            if (holdings.Count == 0)
            {
                await Response().Error("Empty portfolio! Use `.crypto buy <coin> <amount>` to invest.").SendAsync();
                return;
            }

            var coins = await _service.ListCoinsAsync(ctx.Guild.Id);
            var coinPrices = coins.ToDictionary(c => c.Name, c => c.Price);

            var eb = CreateEmbed()
                .WithTitle($"💼 {ctx.User.Username}'s Portfolio")
                .WithOkColor();

            long totalValue = 0;
            foreach (var h in holdings)
            {
                var price = coinPrices.GetValueOrDefault(h.CoinName, 0);
                var value = (long)(h.Amount * price);
                totalValue += value;
                eb.AddField(h.CoinName, $"{h.Amount:F2} units\nValue: {value} 🥠", true);
            }

            eb.WithFooter($"Total Portfolio Value: {totalValue} 🥠");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task History(string coin)
        {
            var coins = await _service.ListCoinsAsync(ctx.Guild.Id);
            var c = coins.FirstOrDefault(x => x.Name.Equals(coin, StringComparison.OrdinalIgnoreCase));
            if (c is null)
            {
                await Response().Error("Unknown coin!").SendAsync();
                return;
            }

            await Response().Confirm($"📊 **{c.Name}**: Current price {c.Price} 🥠 (updated {c.LastUpdated:HH:mm UTC})").SendAsync();
        }
    }
}
