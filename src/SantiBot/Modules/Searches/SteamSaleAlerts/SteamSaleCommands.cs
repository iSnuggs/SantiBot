namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("SteamSaleAlerts")]
    [Group("steamsale")]
    public partial class SteamSaleCommands : SantiModule<SteamSaleAlerts.SteamSaleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SteamSaleWatch([Leftover] string input)
        {
            var parts = input.Split(' ', 2);
            var appIdOrName = parts[0];

            // If it looks like an app ID (all digits), use it directly
            if (!appIdOrName.All(char.IsDigit))
            {
                await Response().Error("Please provide a Steam App ID (numeric). Find it on the Steam store page URL.").SendAsync();
                return;
            }

            var gameName = await _service.ResolveGameNameAsync(appIdOrName);
            var channel = (ITextChannel)ctx.Channel;

            var success = await _service.WatchAsync(ctx.Guild.Id, channel.Id, appIdOrName, gameName);
            if (success)
                await Response()
                    .Confirm($"Now watching **{gameName}** (App {appIdOrName}) for sales in {channel.Mention}")
                    .SendAsync();
            else
                await Response().Error("Already watching that game.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SteamSaleUnwatch(string appId)
        {
            var success = await _service.UnwatchAsync(ctx.Guild.Id, appId);
            if (success)
                await Response().Confirm($"Stopped watching app **{appId}**").SendAsync();
            else
                await Response().Error("Not watching that game.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SteamSaleList()
        {
            var watches = await _service.ListAsync(ctx.Guild.Id);
            if (watches.Count == 0)
            {
                await Response().Error("No Steam sale alerts configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", watches.Select((w, i) =>
                $"`{i + 1}.` {w.GameName} (App {w.AppId}) -> <#{w.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Steam Sale Alerts").WithDescription(desc))
                .SendAsync();
        }
    }
}
