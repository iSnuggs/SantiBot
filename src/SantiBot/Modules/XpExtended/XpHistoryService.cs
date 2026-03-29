#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class XpHistoryService : INService, IReadyExecutor
{
    private readonly DbService _db;

    public XpHistoryService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        // take daily snapshots at midnight
        while (true)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;
            await Task.Delay(delay);

            try
            {
                await TakeDailySnapshotAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task TakeDailySnapshotAsync()
    {
        await using var ctx = _db.GetDbContext();
        var today = DateTime.UtcNow.Date;

        // get all guild xp stats grouped by guild
        var allStats = await ctx.GetTable<UserXpStats>()
            .Where(x => x.Xp > 0)
            .ToListAsyncLinqToDB();

        var grouped = allStats.GroupBy(x => x.GuildId);
        foreach (var guildGroup in grouped)
        {
            var ranked = guildGroup.OrderByDescending(x => x.Xp).ToList();
            for (int i = 0; i < ranked.Count; i++)
            {
                var stat = ranked[i];
                await ctx.GetTable<XpSnapshot>()
                    .InsertAsync(() => new XpSnapshot
                    {
                        GuildId = stat.GuildId,
                        UserId = stat.UserId,
                        Xp = stat.Xp,
                        Rank = i + 1,
                        SnapshotDate = today
                    });
            }
        }
    }

    public async Task<List<XpSnapshot>> GetHistoryAsync(ulong guildId, ulong userId, int days = 30)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpSnapshot>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.SnapshotDate >= since)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<(ulong UserId, int CurrentRank, int PreviousRank)>> GetTopRankChangesAsync(ulong guildId, int days = 7)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        var today = DateTime.UtcNow.Date;

        await using var ctx = _db.GetDbContext();

        var latestSnapshots = await ctx.GetTable<XpSnapshot>()
            .Where(x => x.GuildId == guildId && x.SnapshotDate == today)
            .ToListAsyncLinqToDB();

        var oldSnapshots = await ctx.GetTable<XpSnapshot>()
            .Where(x => x.GuildId == guildId && x.SnapshotDate == since)
            .ToListAsyncLinqToDB();

        var oldLookup = oldSnapshots.ToDictionary(x => x.UserId, x => x.Rank);

        return latestSnapshots
            .Select(x => (x.UserId, x.Rank, oldLookup.GetValueOrDefault(x.UserId, x.Rank)))
            .OrderBy(x => x.Rank)
            .Take(10)
            .ToList();
    }

    public static string FormatRankChange(int current, int previous)
    {
        if (current < previous) return $"\u2191{previous - current}";
        if (current > previous) return $"\u2193{current - previous}";
        return "\u2192";
    }
}
