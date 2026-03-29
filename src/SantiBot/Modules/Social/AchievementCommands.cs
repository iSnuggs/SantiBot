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

            var sb = new System.Text.StringBuilder();
            foreach (var a in earned)
            {
                sb.AppendLine($"{a.Emoji} **{a.AchievementName}** — {a.Description}");
            }

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle($"\U0001F3C6 Achievements ({earned.Count}/{AchievementService.AllAchievements.Count})")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievementlist()
        {
            var all = AchievementService.AllAchievements;
            var earned = await _service.GetAchievementsAsync(ctx.Guild.Id, ctx.User.Id);
            var earnedIds = earned.Select(e => e.AchievementId).ToHashSet();

            var sb = new System.Text.StringBuilder();
            foreach (var a in all)
            {
                var check = earnedIds.Contains(a.Id) ? "\u2705" : "\u2B1C";
                sb.AppendLine($"{check} {a.Emoji} **{a.Name}** — {a.Desc}");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F4CB All Achievements")
                .WithDescription(sb.ToString())
                .WithFooter($"{earned.Count}/{all.Count} unlocked");

            await Response().Embed(eb).SendAsync();
        }
    }
}
