#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("Prestige")]
    [Group("prestige")]
    public partial class PrestigeCommands : SantiModule<PrestigeService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Prestige()
        {
            var (success, error, newPrestige) = await _service.PrestigeAsync(ctx.Guild.Id, ctx.User.Id);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var stars = PrestigeService.GetPrestigeStars(newPrestige);
            var multiplier = _service.GetXpMultiplier(newPrestige);

            var eb = CreateEmbed()
                .WithTitle("\u2728 PRESTIGE!")
                .WithDescription(
                    $"{ctx.User.Mention} has prestiged!\n\n" +
                    $"**Prestige Level:** {newPrestige} {stars}\n" +
                    $"**XP Multiplier:** {multiplier:F1}x\n\n" +
                    $"Your XP has been reset to 0, but you earn XP faster now!")
                .WithColor(Discord.Color.Gold);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PrestigeInfo()
        {
            var prestige = await _service.GetPrestigeAsync(ctx.Guild.Id, ctx.User.Id);
            var level = prestige?.PrestigeLevel ?? 0;
            var stars = PrestigeService.GetPrestigeStars(level);
            var multiplier = _service.GetXpMultiplier(level);

            var eb = CreateEmbed()
                .WithTitle("\u2B50 Prestige Info")
                .WithDescription(
                    $"**Your Prestige:** {level} {stars}\n" +
                    $"**XP Multiplier:** {multiplier:F1}x\n" +
                    $"**Requirement:** Level 50+\n" +
                    $"**Reward:** +10% XP per prestige level\n\n" +
                    $"Use `.prestige` when you're ready to reset!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PrestigeLeaderboard()
        {
            var top = await _service.GetLeaderboardAsync(ctx.Guild.Id);

            if (top.Count == 0)
            {
                await Response().Confirm("No one has prestiged yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < top.Count; i++)
            {
                var p = top[i];
                var user = await ctx.Guild.GetUserAsync(p.UserId);
                var name = user?.ToString() ?? $"User {p.UserId}";
                var stars = PrestigeService.GetPrestigeStars(p.PrestigeLevel);
                sb.AppendLine($"**#{i + 1}** {name} — Prestige {p.PrestigeLevel} {stars}");
            }

            var eb = CreateEmbed()
                .WithTitle("\u2B50 Prestige Leaderboard")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
