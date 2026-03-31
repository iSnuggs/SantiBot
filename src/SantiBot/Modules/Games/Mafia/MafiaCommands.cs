#nullable disable
using SantiBot.Modules.Games.Mafia;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Mafia")]
    [Group("mafia")]
    public partial class MafiaCommands : SantiModule<MafiaService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            var (success, message) = _service.StartGame(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var (success, message) = _service.JoinGame(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Begin()
        {
            var (success, message) = await _service.BeginGame(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Vote(IUser user)
        {
            var (success, message) = _service.Vote(ctx.Channel.Id, ctx.User.Id, user.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task NightAction(IUser user)
        {
            var (success, message) = await _service.NightAction(ctx.Channel.Id, ctx.User.Id, user.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Dawn()
        {
            var (success, message) = await _service.ResolvePhaseAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Resolve()
        {
            var (success, message) = await _service.ResolvePhaseAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
