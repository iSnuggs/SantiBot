#nullable disable
namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("VoiceStats")]
    [Group("vcstats")]
    public partial class VcStatsCommands : SantiModule<VoiceStatService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Vcstats(IUser user = null)
        {
            user ??= ctx.User;
            var stats = await _service.GetVoiceStatsAsync(ctx.Guild.Id, user.Id);

            if (stats is null)
            {
                await Response().Confirm($"{user} has no voice stats yet!").SendAsync();
                return;
            }

            var topPartners = await _service.GetTopPartnersAsync(ctx.Guild.Id, user.Id);

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle("\U0001F3A4 Voice Chat Stats")
                .AddField("Total Time", $"{stats.TotalMinutes / 60:N0}h {stats.TotalMinutes % 60}m", true)
                .AddField("Favorite Channel",
                    stats.FavoriteChannelId != 0 ? $"<#{stats.FavoriteChannelId}>" : "None", true);

            if (topPartners.Count > 0)
            {
                var partnerStr = new System.Text.StringBuilder();
                foreach (var p in topPartners.Take(5))
                {
                    var partnerId = p.User1Id == user.Id ? p.User2Id : p.User1Id;
                    var partner = await ctx.Guild.GetUserAsync(partnerId);
                    var name = partner?.ToString() ?? $"User {partnerId}";
                    partnerStr.AppendLine($"\u2022 {name} — {p.SharedMinutes / 60:N0}h {p.SharedMinutes % 60}m");
                }
                eb.AddField("Top VC Partners", partnerStr.ToString());
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Vcleaderboard()
        {
            var top = await _service.GetVoiceLeaderboardAsync(ctx.Guild.Id);

            if (top.Count == 0)
            {
                await Response().Confirm("No voice stats yet!").SendAsync();
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < top.Count; i++)
            {
                var s = top[i];
                var user = await ctx.Guild.GetUserAsync(s.UserId);
                var name = user?.ToString() ?? $"User {s.UserId}";
                sb.AppendLine($"**#{i + 1}** {name} — {s.TotalMinutes / 60:N0}h {s.TotalMinutes % 60}m");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F3A4 Voice Leaderboard")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
