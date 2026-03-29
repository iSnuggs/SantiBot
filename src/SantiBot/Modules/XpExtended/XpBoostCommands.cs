#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("XpBoost")]
    [Group("xpboost")]
    public partial class XpBoostCommands : SantiModule<XpBoosterService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Xpboost(double multiplier, string duration, IUser user = null)
        {
            if (multiplier < 1.0 || multiplier > 10.0)
            {
                await Response().Error("Multiplier must be between 1.0 and 10.0!").SendAsync();
                return;
            }

            var ts = ParseDuration(duration);
            if (ts is null)
            {
                await Response().Error("Invalid duration! Use: 1h, 6h, 1d, 7d, etc.").SendAsync();
                return;
            }

            var userId = user?.Id ?? 0; // 0 = server-wide
            await _service.SetBoostAsync(ctx.Guild.Id, userId, multiplier, ts.Value);

            var target = user is not null ? user.Mention : "**Server-wide**";
            await Response().Confirm(
                $"\U0001F680 {multiplier}x XP boost activated for {target}! Expires in {duration}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpboostStatus()
        {
            var boosters = await _service.GetActiveBoostersAsync(ctx.Guild.Id);

            if (boosters.Count == 0)
            {
                await Response().Confirm("No active XP boosters.").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var b in boosters)
            {
                var target = b.UserId == 0 ? "Server-wide" : $"<@{b.UserId}>";
                var remaining = b.ExpiresAt - DateTime.UtcNow;
                sb.AppendLine($"\U0001F680 **{b.Multiplier}x** for {target} — {remaining.Hours}h {remaining.Minutes}m remaining");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F680 Active XP Boosters")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        private static TimeSpan? ParseDuration(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim().ToLowerInvariant();
            if (input.EndsWith("h") && int.TryParse(input[..^1], out var h)) return TimeSpan.FromHours(h);
            if (input.EndsWith("d") && int.TryParse(input[..^1], out var d)) return TimeSpan.FromDays(d);
            if (input.EndsWith("m") && int.TryParse(input[..^1], out var m)) return TimeSpan.FromMinutes(m);
            return null;
        }
    }
}
