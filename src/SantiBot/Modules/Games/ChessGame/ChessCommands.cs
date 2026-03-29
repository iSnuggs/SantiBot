#nullable disable
using SantiBot.Modules.Games.ChessGame;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Chess")]
    [Group("chess")]
    public partial class ChessCommands : SantiModule<ChessService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Challenge(IUser opponent)
        {
            var (success, message) = _service.Challenge(ctx.Channel.Id, ctx.User.Id, ctx.User.Username, opponent.Id, opponent.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Move(string from, string to)
        {
            var (success, message) = await _service.MoveAsync(ctx.Channel.Id, ctx.User.Id, from?.ToLower(), to?.ToLower());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Resign()
        {
            var (success, message) = await _service.ResignAsync(ctx.Channel.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Board()
        {
            var board = _service.GetBoard(ctx.Channel.Id);
            if (board is null)
            {
                await Response().Error("No chess game in this channel!").SendAsync();
                return;
            }

            await Response().Confirm($"♟️ **Current Board**\n{board}").SendAsync();
        }
    }
}
