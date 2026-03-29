#nullable disable
using SantiBot.Modules.Games.PuzzleDay;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Puzzle")]
    [Group("puzzle")]
    public partial class PuzzleCommands : SantiModule<PuzzleService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Puzzle()
        {
            var (question, number) = _service.GetPuzzle();
            var eb = CreateEmbed()
                .WithTitle($"🧩 Puzzle of the Day (#{number})")
                .WithDescription(question)
                .WithFooter("Use `.puzzle solve <answer>` to answer, `.puzzle hint` for a hint")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Solve([Leftover] string answer)
        {
            var (success, message) = await _service.SolveAsync(ctx.Guild.Id, ctx.User.Id, answer?.Trim());
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hint()
        {
            var hint = _service.GetHint();
            await Response().Confirm($"💡 Hint: {hint}").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard()
        {
            var lb = await _service.GetLeaderboardAsync(ctx.Guild.Id);
            if (lb.Count == 0)
            {
                await Response().Error("No puzzle scores yet!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🧩 Puzzle Leaderboard")
                .WithOkColor();

            for (int i = 0; i < lb.Count; i++)
            {
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"#{i + 1}" };
                eb.AddField($"{medal} <@{lb[i].UserId}>", $"Points: {lb[i].TotalPoints} | Solved: {lb[i].TotalSolved}", false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
