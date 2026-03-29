#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("XpHistory")]
    [Group("xphistory")]
    public partial class XpHistoryCommands : SantiModule<XpHistoryService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Xphistory(IUser user = null, int days = 30)
        {
            user ??= ctx.User;
            days = Math.Clamp(days, 7, 90);

            var history = await _service.GetHistoryAsync(ctx.Guild.Id, user.Id, days);

            if (history.Count == 0)
            {
                await Response().Confirm("No XP history available yet. Snapshots are taken daily.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine($"XP History (last {days} days)");
            sb.AppendLine();

            for (int i = 0; i < history.Count; i++)
            {
                var h = history[i];
                var change = i > 0 ? XpHistoryService.FormatRankChange(h.Rank, history[i - 1].Rank) : "\u2192";
                sb.AppendLine($"{h.SnapshotDate:MMM dd} | Rank #{h.Rank} {change} | {h.Xp:N0} XP");
            }
            sb.AppendLine("```");

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("\U0001F4C8 XP History")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XphistoryTop(int days = 7)
        {
            days = Math.Clamp(days, 1, 30);
            var changes = await _service.GetTopRankChangesAsync(ctx.Guild.Id, days);

            if (changes.Count == 0)
            {
                await Response().Confirm("No ranking history available yet.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var (userId, current, previous) in changes)
            {
                var user = await ctx.Guild.GetUserAsync(userId);
                var name = user?.ToString() ?? $"User {userId}";
                var change = XpHistoryService.FormatRankChange(current, previous);
                sb.AppendLine($"**#{current}** {name} {change}");
            }

            var eb = CreateEmbed()
                .WithTitle($"\U0001F4CA Top Rank Changes (last {days} days)")
                .WithDescription(sb.ToString())
                .WithFooter("\u2191 = climbed  \u2193 = dropped  \u2192 = same");

            await Response().Embed(eb).SendAsync();
        }
    }
}
