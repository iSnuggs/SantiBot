#nullable disable
using SantiBot.Modules.Utility.Achievements;
using System.Text;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    // Standalone — responds to just .achievements (no subcommand needed)
    [Name("Achievements")]
    public partial class AchievementsBase(AchievementService ach) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievements()
            => await ShowAchievements(ctx.User);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievements(IUser user)
            => await ShowAchievements(user);

        private async Task ShowAchievements(IUser user)
        {
            var unlocked = await ach.GetUserAchievementsAsync(user.Id);

            if (unlocked.Count == 0)
            {
                await Response().Error(strs.achievements_none(user.Mention)).SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var ua in unlocked)
            {
                var def = AchievementService.GetDef(ua.AchievementId);
                if (def is null)
                    continue;

                sb.AppendLine($"{def.Emoji} **{def.Name}** - {def.Description}");
                sb.AppendLine($"  Unlocked: {ua.UnlockedAt:MMM dd, yyyy}");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{user.Username}'s Achievements ({unlocked.Count}/{AchievementService.AllAchievements.Length})")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }

    // Group — responds to .achievements list
    [Group("achievements")]
    [Name("Achievements")]
    public partial class AchievementCommands(AchievementService ach) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AchievementsList()
        {
            var userAch = await ach.GetUserAchievementsAsync(ctx.User.Id);
            var unlockedIds = userAch.Select(a => a.AchievementId).ToHashSet();

            var sb = new StringBuilder();
            foreach (var group in AchievementService.GetAllGrouped())
            {
                sb.AppendLine($"**{group.Key}**");
                foreach (var def in group)
                {
                    var check = unlockedIds.Contains(def.Id) ? "+" : "-";
                    sb.AppendLine($"  {def.Emoji} [{check}] {def.Name} - {def.Description}");
                }
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("All Achievements")
                .WithDescription(sb.ToString())
                .WithFooter($"[+] = unlocked | [-] = locked | {unlockedIds.Count}/{AchievementService.AllAchievements.Length} unlocked");

            await Response().Embed(eb).SendAsync();
        }
    }
}
