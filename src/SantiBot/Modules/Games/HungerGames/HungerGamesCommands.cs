#nullable disable
using SantiBot.Modules.Games.HungerGames;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("HungerGames")]
    [Group("hg")]
    public partial class HungerGamesCommands : SantiModule<HungerGamesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            var (success, message) = _service.StartGame(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var (success, message) = _service.Join(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Volunteer(IUser user)
        {
            var (success, message) = _service.Volunteer(ctx.Channel.Id, user.Id, user.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Go()
        {
            var (success, message) = await _service.RunRoundAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
