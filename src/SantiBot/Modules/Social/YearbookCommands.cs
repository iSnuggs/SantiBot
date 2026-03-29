#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Yearbook")]
    [Group("yearbook")]
    public partial class YearbookCommands : SantiModule
    {
        private readonly DbService _db;

        public YearbookCommands(DbService db)
        {
            _db = db;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Yearbook(IUser user = null, int year = 0)
        {
            user ??= ctx.User;
            if (year == 0) year = DateTime.UtcNow.Year;

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year + 1, 1, 1);

            await using var dbCtx = _db.GetDbContext();

            // message stats from heatmap
            var heatmapData = await dbCtx.GetTable<ActivityHeatmap>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.UserId == user.Id
                    && x.Date >= startDate && x.Date < endDate)
                .ToListAsyncLinqToDB();

            var totalMessages = heatmapData.Sum(x => x.MessageCount);
            var activeDays = heatmapData.Count;
            var busiestDay = heatmapData.OrderByDescending(x => x.MessageCount).FirstOrDefault();

            // voice stats
            var voiceStats = await dbCtx.GetTable<VoiceStat>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.UserId == user.Id)
                .FirstOrDefaultAsyncLinqToDB();

            // karma
            var karma = await dbCtx.GetTable<UserKarma>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.UserId == user.Id)
                .FirstOrDefaultAsyncLinqToDB();

            // friends
            var friends = await dbCtx.GetTable<Friendship>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.Accepted
                    && (x.User1Id == user.Id || x.User2Id == user.Id))
                .CountAsyncLinqToDB();

            // achievements
            var achievements = await dbCtx.GetTable<UserAchievement>()
                .Where(x => x.GuildId == ctx.Guild.Id && x.UserId == user.Id)
                .CountAsyncLinqToDB();

            var eb = CreateEmbed()
                .WithAuthor(user.ToString(), user.GetAvatarUrl())
                .WithTitle($"\U0001F4D6 {year} Yearbook")
                .AddField("\U0001F4AC Messages", totalMessages.ToString("N0"), true)
                .AddField("\U0001F4C5 Active Days", activeDays.ToString(), true)
                .AddField("\U0001F525 Busiest Day",
                    busiestDay is not null
                        ? $"{busiestDay.Date:MMM dd} ({busiestDay.MessageCount} msgs)"
                        : "N/A", true)
                .AddField("\U0001F3A4 Voice Hours",
                    voiceStats is not null ? $"{voiceStats.TotalMinutes / 60:N0}h" : "0h", true)
                .AddField("\u2728 Karma",
                    karma is not null ? $"{karma.Upvotes - karma.Downvotes:N0}" : "0", true)
                .AddField("\U0001F46B Friends", friends.ToString(), true)
                .AddField("\U0001F3C6 Achievements", achievements.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
