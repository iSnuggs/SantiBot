#nullable disable
using SantiBot.Modules.Games.MinigameMashup;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Minigame")]
    [Group("minigame")]
    public partial class MinigameCommands : SantiModule<MinigameService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Minigame()
        {
            var (success, challenge) = _service.StartMinigame(ctx.Channel.Id);
            if (success)
                await Response().Confirm(challenge).SendAsync();
            else
                await Response().Error(challenge).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Mg()
        {
            var (success, challenge) = _service.StartMinigame(ctx.Channel.Id);
            if (success)
                await Response().Confirm(challenge).SendAsync();
            else
                await Response().Error(challenge).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Answer([Leftover] string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return;
            var (won, message) = await _service.TryAnswerAsync(ctx.Channel.Id, ctx.User.Id, answer);
            if (won)
                await Response().Confirm(message).SendAsync();
        }
    }
}
