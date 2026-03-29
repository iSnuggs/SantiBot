#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("SocialBoard")]
    [Group("socialboard")]
    public partial class SocialLeaderboardCommands : SantiModule<SocialStatService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Socialboard([Leftover] string type = "messages")
        {
            type = type?.ToLowerInvariant()?.Trim() ?? "messages";
            var isWeekly = type.Contains("weekly");
            type = type.Replace("weekly", "").Trim();

            if (string.IsNullOrEmpty(type)) type = "messages";

            var validTypes = new[] { "messages", "reactions", "voice", "karma", "helpful" };
            if (!validTypes.Contains(type))
            {
                await Response().Error($"Valid types: {string.Join(", ", validTypes)}. Add 'weekly' for weekly stats.").SendAsync();
                return;
            }

            var top = await _service.GetLeaderboardAsync(ctx.Guild.Id, type, isWeekly);

            if (top.Count == 0)
            {
                await Response().Confirm("No social stats yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < top.Count; i++)
            {
                var s = top[i];
                var user = await ctx.Guild.GetUserAsync(s.UserId);
                var name = user?.ToString() ?? $"User {s.UserId}";

                var value = (type, isWeekly) switch
                {
                    ("messages", false) => s.TotalMessages,
                    ("messages", true) => s.WeeklyMessages,
                    ("reactions", false) => s.TotalReactions,
                    ("reactions", true) => s.WeeklyReactions,
                    ("voice", false) => s.TotalVoiceMinutes,
                    ("voice", true) => s.WeeklyVoiceMinutes,
                    ("helpful", _) => s.HelpfulReactions,
                    _ => s.TotalMessages
                };

                var unit = type == "voice" ? "min" : "";
                sb.AppendLine($"**#{i + 1}** {name} — {value:N0} {unit}");
            }

            var title = $"\U0001F3C6 {(isWeekly ? "Weekly " : "")}{char.ToUpper(type[0])}{type[1..]} Leaderboard";
            var eb = CreateEmbed()
                .WithTitle(title)
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
