#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.Seasons;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Season")]
    [Group("season")]
    public partial class SeasonCommands : SantiModule<SeasonService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Info()
        {
            var season = await _service.GetActiveSeasonAsync(ctx.Guild.Id);
            var duration = DateTime.UtcNow - season.StartedAt;

            var eb = CreateEmbed()
                .WithTitle($"📅 Season {season.SeasonNumber}")
                .AddField("Started", season.StartedAt.ToString("yyyy-MM-dd"), true)
                .AddField("Running For", $"{duration.Days} days", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard()
        {
            var top = await _service.GetLeaderboardAsync(ctx.Guild.Id);
            if (top.Count == 0)
            {
                await Response().Error("No earnings this season yet!").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < top.Count; i++)
            {
                var medal = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"#{i + 1}" };
                sb.AppendLine($"{medal} <@{top[i].UserId}> — {top[i].TotalEarned} 🥠");
            }

            var eb = CreateEmbed()
                .WithTitle("🏆 Season Leaderboard")
                .WithDescription(sb.ToString())
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task History()
        {
            var seasons = await _service.GetHistoryAsync(ctx.Guild.Id);
            if (seasons.Count == 0)
            {
                await Response().Error("No past seasons.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var s in seasons)
                sb.AppendLine($"Season {s.SeasonNumber}: {s.StartedAt:yyyy-MM-dd} to {s.EndedAt:yyyy-MM-dd}");

            await Response().Confirm($"📜 Season History\n```\n{sb}\n```").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Reset()
        {
            var (success, message) = await _service.ResetSeasonAsync(ctx.Guild.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
