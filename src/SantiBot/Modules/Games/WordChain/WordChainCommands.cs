#nullable disable
using SantiBot.Modules.Games.WordChain;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("WordChain")]
    [Group("wordchain")]
    public partial class WordChainCommands : SantiModule<WordChainService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            var (success, message) = _service.StartGame(ctx.Channel.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Stop()
        {
            _service.StopGame(ctx.Channel.Id);
            await Response().Confirm("Word chain game stopped!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Play([Leftover] string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            var (valid, message, _) = await _service.SubmitWordAsync(ctx.Channel.Id, ctx.User.Id, word.Split(' ')[0]);
            if (!string.IsNullOrEmpty(message))
            {
                if (valid)
                    await Response().Confirm(message).SendAsync();
                else
                    await Response().Error(message).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var (success, message) = _service.JoinGame(ctx.Channel.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
