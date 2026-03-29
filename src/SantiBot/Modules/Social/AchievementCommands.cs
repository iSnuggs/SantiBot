#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Achievements")]
    [Group("achievements")]
    public partial class AchievementCommands : SantiModule<AchievementService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievements(IUser user = null)
        {
            user ??= ctx.User;
            var earned = await _service.GetAchievementsAsync(ctx.Guild.Id, user.Id);

            if (earned.Count == 0)
            {
                await Response().Confirm($"{user} hasn't earned any achievements yet!").SendAsync();
                return;
            }

            // Group by category
            var grouped = earned
                .GroupBy(a => AchievementService.AllAchievements
                    .FirstOrDefault(x => x.Id == a.AchievementId).Category ?? "Other")
                .OrderByDescending(g => g.Count());

            var sb = new System.Text.StringBuilder();
            foreach (var group in grouped)
            {
                sb.AppendLine($"**{group.Key}** ({group.Count()})");
                foreach (var a in group.Take(5))
                    sb.AppendLine($"  {a.Emoji} {a.AchievementName}");
                if (group.Count() > 5)
                    sb.AppendLine($"  *...and {group.Count() - 5} more*");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle($"🏆 Achievements ({earned.Count}/{AchievementService.AllAchievements.Count})")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievementlist([Leftover] string category = null)
        {
            var all = AchievementService.AllAchievements;
            var earned = await _service.GetAchievementsAsync(ctx.Guild.Id, ctx.User.Id);
            var earnedIds = earned.Select(e => e.AchievementId).ToHashSet();

            if (string.IsNullOrWhiteSpace(category))
            {
                // Show category overview
                var categories = all.GroupBy(a => a.Category).OrderBy(g => g.Key);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("**Achievement Categories:**\n");
                foreach (var cat in categories)
                {
                    var catEarned = cat.Count(a => earnedIds.Contains(a.Id));
                    var pct = cat.Count() > 0 ? catEarned * 100 / cat.Count() : 0;
                    var bar = new string('█', pct / 10) + new string('░', 10 - pct / 10);
                    sb.AppendLine($"[{bar}] **{cat.Key}** — {catEarned}/{cat.Count()} ({pct}%)");
                }
                sb.AppendLine($"\n**Total: {earnedIds.Count}/{all.Count}**");
                sb.AppendLine("\nUse `.achievements list <category>` to see details!");

                var eb = CreateEmbed()
                    .WithTitle("📋 Achievement Categories")
                    .WithDescription(sb.ToString())
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
                return;
            }

            // Show specific category
            var filtered = all.Where(a =>
                a.Category.Contains(category, System.StringComparison.OrdinalIgnoreCase)).ToList();

            if (filtered.Count == 0)
            {
                await Response().Error($"Unknown category! Use `.achievements list` to see all categories.").SendAsync();
                return;
            }

            var catName = filtered.First().Category;
            var desc = new System.Text.StringBuilder();
            foreach (var a in filtered)
            {
                var check = earnedIds.Contains(a.Id) ? "✅" : "⬜";
                // Hide secret achievement descriptions if not earned
                if (a.Category == "Secret" && !earnedIds.Contains(a.Id))
                    desc.AppendLine($"{check} ❓ **???** — *Hidden*");
                else
                    desc.AppendLine($"{check} {a.Emoji} **{a.Name}** — {a.Desc}");
            }

            var catEarnedCount = filtered.Count(a => earnedIds.Contains(a.Id));
            var embed = CreateEmbed()
                .WithTitle($"📋 {catName} Achievements")
                .WithDescription(desc.ToString())
                .WithFooter($"{catEarnedCount}/{filtered.Count} unlocked")
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievementstats()
        {
            var all = AchievementService.AllAchievements;
            var earned = await _service.GetAchievementsAsync(ctx.Guild.Id, ctx.User.Id);
            var pct = all.Count > 0 ? earned.Count * 100 / all.Count : 0;
            var bar = new string('█', pct / 5) + new string('░', 20 - pct / 5);

            var recent = earned.OrderByDescending(a => a.DateAdded).Take(5);
            var recentSb = new System.Text.StringBuilder();
            foreach (var a in recent)
                recentSb.AppendLine($"{a.Emoji} **{a.AchievementName}**");

            var eb = CreateEmbed()
                .WithAuthor(ctx.User.ToString(), ctx.User.GetAvatarUrl())
                .WithTitle("🏆 Achievement Stats")
                .AddField("Progress", $"[{bar}] {earned.Count}/{all.Count} ({pct}%)", false)
                .AddField("Recent Unlocks", recentSb.Length > 0 ? recentSb.ToString() : "None yet!", false)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
