#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class HeatmapService : INService
{
    private readonly DbService _db;

    public HeatmapService(DbService db)
    {
        _db = db;
    }

    public async Task RecordActivityAsync(ulong guildId, ulong userId)
    {
        var today = DateTime.UtcNow.Date;
        await using var ctx = _db.GetDbContext();

        var updated = await ctx.GetTable<ActivityHeatmap>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.Date == today)
            .Set(x => x.MessageCount, x => x.MessageCount + 1)
            .UpdateAsync();

        if (updated == 0)
        {
            await ctx.GetTable<ActivityHeatmap>()
                .InsertAsync(() => new ActivityHeatmap
                {
                    GuildId = guildId,
                    UserId = userId,
                    Date = today,
                    MessageCount = 1
                });
        }
    }

    public async Task<List<ActivityHeatmap>> GetHeatmapAsync(ulong guildId, ulong userId, int days)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ActivityHeatmap>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.Date >= since)
            .OrderBy(x => x.Date)
            .ToListAsyncLinqToDB();
    }

    public static string RenderHeatmap(List<ActivityHeatmap> data, int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var lookup = data.ToDictionary(x => x.Date.Date, x => x.MessageCount);
        var maxCount = data.Count > 0 ? data.Max(x => x.MessageCount) : 1;
        if (maxCount == 0) maxCount = 1;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("```");
        sb.AppendLine("Activity Heatmap (last " + days + " days)");
        sb.AppendLine();

        // render weeks as rows, days as columns
        var weeks = (days + 6) / 7;
        for (int w = 0; w < weeks && w < 15; w++)
        {
            for (int d = 0; d < 7; d++)
            {
                var date = startDate.AddDays(w * 7 + d);
                if (date > DateTime.UtcNow.Date) { sb.Append(' '); continue; }

                lookup.TryGetValue(date, out var count);
                var intensity = (double)count / maxCount;
                var block = intensity switch
                {
                    0 => ' ',
                    < 0.25 => '\u2591', // light
                    < 0.50 => '\u2592', // medium
                    < 0.75 => '\u2593', // dark
                    _ => '\u2588'        // full
                };
                sb.Append(block);
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("  = none  \u2591 = low  \u2592 = medium  \u2593 = high  \u2588 = max");
        sb.AppendLine("```");
        return sb.ToString();
    }
}
