namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Reputation")]
    [Group("rep")]
    public partial class ReputationCommands : SantiModule<ReputationService>
    {
        [Cmd]
        [Priority(1)]
        public async Task Rep(IUser target)
        {
            if (target.Id == ctx.User.Id)
            {
                await Response().Error(strs.rep_self).SendAsync();
                return;
            }

            if (target.IsBot)
            {
                await Response().Error(strs.rep_bot).SendAsync();
                return;
            }

            var cooldown = await _service.GiveRepAsync(ctx.Guild.Id, ctx.User.Id, target.Id);

            if (cooldown == TimeSpan.Zero)
            {
                // Sentinel for self-rep (shouldn't reach here due to above check)
                await Response().Error(strs.rep_self).SendAsync();
                return;
            }

            if (cooldown is TimeSpan remaining)
            {
                await Response()
                    .Error(strs.rep_cooldown(remaining.Hours, remaining.Minutes))
                    .SendAsync();
                return;
            }

            var newCount = await _service.GetRepAsync(ctx.Guild.Id, target.Id);
            await Response()
                .Confirm(strs.rep_given(target.Mention, newCount))
                .SendAsync();
        }

        [Cmd]
        [Priority(0)]
        public async Task Rep()
        {
            var count = await _service.GetRepAsync(ctx.Guild.Id, ctx.User.Id);

            await Response()
                .Confirm(strs.rep_count(ctx.User.Mention, count))
                .SendAsync();
        }

        [Cmd]
        public async Task RepLeaderboard()
        {
            var leaders = await _service.GetLeaderboardAsync(ctx.Guild.Id);

            if (leaders.Count == 0)
            {
                await Response().Error(strs.rep_nobody).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.rep_leaderboard));

            for (var i = 0; i < leaders.Count; i++)
            {
                var entry = leaders[i];
                eb.AddField($"#{i + 1}", $"<@{entry.UserId}> — **{entry.RepCount}** rep", false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
