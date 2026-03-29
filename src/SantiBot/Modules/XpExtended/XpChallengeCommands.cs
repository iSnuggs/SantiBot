#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("XpChallenge")]
    [Group("xpchallenge")]
    public partial class XpChallengeCommands : SantiModule<XpChallengeService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Xpchallenge()
        {
            var challenges = await _service.GetActiveChallengesAsync(ctx.Guild.Id);

            if (challenges.Count == 0)
            {
                await Response().Confirm("No active challenges!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F3AF Weekly XP Challenges");

            foreach (var c in challenges)
            {
                var remaining = c.EndDate - DateTime.UtcNow;
                eb.AddField(
                    $"{c.Description}",
                    $"Reward: **{c.BonusXp:N0} XP** | Ends in: {remaining.Days}d {remaining.Hours}h",
                    false);
            }

            eb.WithFooter("Progress auto-tracked. Check with .xpchallenge progress");
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpchallengeProgress()
        {
            var progress = await _service.GetUserProgressAsync(ctx.Guild.Id, ctx.User.Id);

            if (progress.Count == 0)
            {
                await Response().Confirm("No active challenges!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F4CA Your Challenge Progress");

            foreach (var (challenge, prog) in progress)
            {
                var percent = challenge.TargetAmount > 0
                    ? Math.Min(100, (int)((double)prog.CurrentAmount / challenge.TargetAmount * 100))
                    : 0;
                var barLen = 15;
                var filled = (int)((double)percent / 100 * barLen);
                var bar = new string('\u2588', filled) + new string('\u2591', barLen - filled);

                var status = prog.Completed ? "\u2705 Complete!" : $"{prog.CurrentAmount}/{challenge.TargetAmount}";
                eb.AddField(
                    challenge.Description,
                    $"`[{bar}]` {percent}% — {status}\nReward: {challenge.BonusXp:N0} XP",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpchallengeList()
        {
            var challenges = await _service.GetActiveChallengesAsync(ctx.Guild.Id);
            var userProgress = await _service.GetUserProgressAsync(ctx.Guild.Id, ctx.User.Id);

            var eb = CreateEmbed()
                .WithTitle("\U0001F4CB All Active Challenges");

            foreach (var (challenge, prog) in userProgress)
            {
                var status = prog.Completed ? "\u2705" : "\u2B1C";
                eb.AddField(
                    $"{status} {challenge.Description}",
                    $"Type: {challenge.ChallengeType} | Target: {challenge.TargetAmount} | Bonus: {challenge.BonusXp:N0} XP",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
