#nullable disable
using SantiBot.Modules.Games.Racing;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Racing")]
    [Group("race")]
    public partial class RacingCommands : SantiModule<RacingService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join(long bet = 0)
        {
            var (success, message) = await _service.JoinRace(ctx.Channel.Id, ctx.User.Id, ctx.User.Username, bet);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            var (success, message) = await _service.StartRace(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Garage()
        {
            var car = await _service.GetGarageAsync(ctx.User.Id);
            var eb = CreateEmbed()
                .WithTitle($"🏎️ {ctx.User.Username}'s Garage")
                .AddField("Speed", car.Speed.ToString(), true)
                .AddField("Handling", car.Handling.ToString(), true)
                .AddField("Nitro", car.Nitro.ToString(), true)
                .AddField("Wins", car.Wins.ToString(), true)
                .AddField("Races", car.Races.ToString(), true)
                .AddField("Win Rate", car.Races > 0 ? $"{car.Wins * 100 / car.Races}%" : "N/A", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Upgrade([Leftover] string stat)
        {
            var (success, message) = await _service.UpgradeAsync(ctx.User.Id, stat?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
