#nullable disable
using SantiBot.Modules.Games.TriviaTournament;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("TriviaTournament")]
    [Group("triviatour")]
    public partial class TriviaTournamentCommands : SantiModule<TriviaTournamentService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start(string category = "General", long entryFee = 0)
        {
            var (success, message) = _service.StartTournament(ctx.Channel.Id, category, entryFee, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Join()
        {
            var (success, message) = await _service.JoinTournament(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Next()
        {
            var (success, question) = _service.NextQuestion(ctx.Channel.Id);
            if (success)
                await Response().Confirm(question).SendAsync();
            else
                await Response().Error(question).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Answer([Leftover] string answer)
        {
            var (correct, message) = await _service.AnswerQuestion(ctx.Channel.Id, ctx.User.Id, answer?.Trim());
            if (correct)
                await Response().Confirm(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard()
        {
            var lb = await _service.GetLeaderboardAsync(ctx.Guild.Id);
            if (lb.Count == 0)
            {
                await Response().Error("No trivia tournament data yet!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🏆 Trivia Tournament Leaderboard")
                .WithOkColor();

            for (int i = 0; i < lb.Count; i++)
                eb.AddField($"#{i + 1} {lb[i].Username}", $"Wins: {lb[i].Wins} | Score: {lb[i].TotalScore}", false);

            await Response().Embed(eb).SendAsync();
        }
    }
}
