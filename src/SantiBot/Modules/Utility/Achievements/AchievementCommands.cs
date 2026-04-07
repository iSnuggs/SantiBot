#nullable disable
using SantiBot.Modules.Utility.Achievements;
using System.Text;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    // Standalone — responds to just .achievements (no subcommand needed, self only)
    [Name("Achievements")]
    public partial class AchievementsBase(AchievementService ach) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Achievements()
            => await ShowAchievements(ctx.User);

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

    // Group — responds to .achievements list / .achievements stats / .achievements lb / .achievements show @user
    [Group("achievements")]
    [Name("Achievements")]
    public partial class AchievementCommands(AchievementService ach) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AchievementsShow(IUser user)
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
                if (def is null) continue;
                sb.AppendLine($"{def.Emoji} **{def.Name}** - {def.Description}");
                sb.AppendLine($"  Unlocked: {ua.UnlockedAt:MMM dd, yyyy}");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{user.Username}'s Achievements ({unlocked.Count}/{AchievementService.AllAchievements.Length})")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

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

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AchievementsStats()
        {
            var unlocked = await ach.GetUserAchievementsAsync(ctx.User.Id);
            var unlockedIds = unlocked.Select(a => a.AchievementId).ToHashSet();
            var total = AchievementService.AllAchievements.Length;
            var count = unlockedIds.Count;
            var pct = total == 0 ? 0 : (int)Math.Round((double)count / total * 100);

            var sb = new StringBuilder();
            sb.AppendLine($"**Overall:** {count}/{total} ({pct}%)");
            sb.AppendLine();

            foreach (var group in AchievementService.GetAllGrouped())
            {
                var groupTotal = group.Count();
                var groupUnlocked = group.Count(d => unlockedIds.Contains(d.Id));
                var bar = BuildBar(groupUnlocked, groupTotal);
                sb.AppendLine($"**{group.Key}** {bar} {groupUnlocked}/{groupTotal}");
            }

            if (unlocked.Count > 0)
            {
                var latest = unlocked.OrderByDescending(a => a.UnlockedAt).First();
                var latestDef = AchievementService.GetDef(latest.AchievementId);
                if (latestDef is not null)
                    sb.AppendLine($"\n🕐 **Latest:** {latestDef.Emoji} {latestDef.Name} ({latest.UnlockedAt:MMM dd, yyyy})");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"🏆 {ctx.User.Username}'s Achievement Stats")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task AchievementsLb()
        {
            var lb = await ach.GetLeaderboardAsync(ctx.Guild.Id);
            var total = AchievementService.AllAchievements.Length;

            if (lb.Count == 0)
            {
                await Response().Error("No one in this server has unlocked any achievements yet!").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            var medals = new[] { "🥇", "🥈", "🥉" };

            for (int i = 0; i < lb.Count; i++)
            {
                var (userId, count) = lb[i];
                var user = await ctx.Guild.GetUserAsync(userId);
                var name = user?.Username ?? $"User {userId}";
                var medal = i < medals.Length ? medals[i] : $"#{i + 1}";
                var pct = (int)Math.Round((double)count / total * 100);
                sb.AppendLine($"{medal} **{name}** — {count}/{total} ({pct}%)");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("🏆 Achievement Leaderboard")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        private static string BuildBar(int value, int max, int width = 8)
        {
            if (max == 0) return new string('░', width);
            var filled = (int)Math.Round((double)value / max * width);
            return new string('█', filled) + new string('░', width - filled);
        }
    }
}
