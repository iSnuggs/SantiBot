#nullable disable
using SantiBot.Modules.Games.IdleClicker;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Idle")]
    [Group("idle")]
    public partial class IdleCommands : SantiModule<IdleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var (player, offline) = await _service.GetStatusAsync(ctx.User.Id);

            var eb = CreateEmbed()
                .WithTitle($"🏭 {ctx.User.Username}'s Idle Game")
                .AddField("Resources", $"{player.Resources + offline:N0}", true)
                .AddField("Per Second", $"{player.ResourcesPerSecond * player.PrestigeMultiplier:F1}", true)
                .AddField("Click Power", $"{(int)(player.ClickPower * player.PrestigeMultiplier)}", true)
                .AddField("Prestige", $"Lv{player.PrestigeLevel} ({player.PrestigeMultiplier:F1}x)", true)
                .AddField("Offline Earnings", $"+{offline:N0}", true)
                .WithFooter("Use `.idle click` to earn, `.idle buy <upgrade>` to upgrade, `.idle prestige` at 10k resources")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Click()
        {
            var (success, message, _) = await _service.ClickAsync(ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Buy([Leftover] string upgrade)
        {
            var (success, message) = await _service.BuyUpgradeAsync(ctx.User.Id, upgrade?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Prestige()
        {
            var (success, message) = await _service.PrestigeAsync(ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Shop()
        {
            var eb = CreateEmbed()
                .WithTitle("🛒 Idle Upgrades Shop")
                .WithOkColor();

            foreach (var (name, info) in IdleService.Upgrades)
            {
                var desc = "";
                if (info.RpsBoost > 0) desc += $"+{info.RpsBoost:F1} RPS ";
                if (info.ClickBoost > 0) desc += $"+{info.ClickBoost} Click ";
                desc += $"| Base cost: {info.Cost}";
                eb.AddField(name, desc, true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
