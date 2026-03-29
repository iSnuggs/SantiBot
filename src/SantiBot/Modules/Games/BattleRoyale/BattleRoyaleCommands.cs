#nullable disable
using SantiBot.Modules.Games.BattleRoyale;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("BattleRoyale")]
    [Group("br")]
    public partial class BattleRoyaleCommands : SantiModule<BattleRoyaleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start(long entryFee = 0)
        {
            var (success, message) = _service.StartGame(ctx.Channel.Id, entryFee);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var (success, message) = await _service.JoinGame(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
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
