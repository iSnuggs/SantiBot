namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("CryptoAlerts")]
    [Group("cryptoalert")]
    public partial class CryptoAlertCommands : SantiModule<CryptoAlerts.CryptoAlertService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CryptoAlertAdd(string coin, string direction, decimal price, ITextChannel channel = null)
        {
            if (direction.ToLower() is not ("above" or "below"))
            {
                await Response().Error("Direction must be `above` or `below`.").SendAsync();
                return;
            }

            channel ??= (ITextChannel)ctx.Channel;
            var id = await _service.AddAlertAsync(ctx.Guild.Id, channel.Id, ctx.User.Id, coin, direction, price);
            await Response()
                .Confirm($"Alert #{id} set: notify when **{coin.ToUpper()}** goes **{direction}** **${price:N2}** in {channel.Mention}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CryptoAlertRemove(int id)
        {
            var success = await _service.RemoveAlertAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"Removed alert #{id}").SendAsync();
            else
                await Response().Error("Alert not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CryptoAlertList()
        {
            var alerts = await _service.ListAlertsAsync(ctx.Guild.Id);
            if (alerts.Count == 0)
            {
                await Response().Error("No active crypto alerts.").SendAsync();
                return;
            }

            var desc = string.Join("\n", alerts.Select(a =>
                $"`#{a.Id}` **{a.CoinId.ToUpper()}** {a.Direction} ${a.TargetPrice:N2} (<@{a.UserId}>) -> <#{a.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Crypto Alerts").WithDescription(desc))
                .SendAsync();
        }
    }
}
