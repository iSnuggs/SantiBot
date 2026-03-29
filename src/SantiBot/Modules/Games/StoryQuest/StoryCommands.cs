#nullable disable
using SantiBot.Modules.Games.StoryQuest;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Quest")]
    [Group("quest")]
    public partial class StoryCommands : SantiModule<StoryService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start([Leftover] string questId = null)
        {
            var (success, message) = await _service.StartQuestAsync(ctx.User.Id, questId?.Trim()?.ToLower());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Continue([Leftover] string choice)
        {
            if (string.IsNullOrWhiteSpace(choice))
            {
                await Response().Error("Specify a choice (A, B, or C)!").SendAsync();
                return;
            }

            var (success, message) = await _service.ContinueQuestAsync(ctx.User.Id, choice.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var (success, message) = await _service.GetStatusAsync(ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Abandon()
        {
            var (success, message) = await _service.AbandonQuestAsync(ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
