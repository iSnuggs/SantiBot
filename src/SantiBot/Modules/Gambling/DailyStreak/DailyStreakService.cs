using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling.DailyStreak;

public sealed class DailyStreakService(ICurrencyService _cur, DbService _db) : INService
{
    private const long BASE_DAILY_REWARD = 100;

    public static double GetMultiplier(int streak)
        => streak switch
        {
            >= 365 => 5.0,
            >= 30 => 3.0,
            >= 7 => 2.0,
            _ => 1.0
        };

    public async Task<(int currentStreak, int longestStreak, long reward, bool alreadyClaimed)> ClaimDailyAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var now = DateTime.UtcNow;

        var existing = await ctx.Set<Db.Models.DailyStreak>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (existing is not null)
        {
            var hoursSinceClaim = (now - existing.LastClaimUtc).TotalHours;

            // Already claimed today (less than 24h)
            if (hoursSinceClaim < 24)
                return (existing.CurrentStreak, existing.LongestStreak, 0, true);

            // Streak broken — more than 48h since last claim
            var newStreak = hoursSinceClaim > 48 ? 1 : existing.CurrentStreak + 1;
            var longest = Math.Max(newStreak, existing.LongestStreak);
            var multiplier = GetMultiplier(newStreak);
            var reward = (long)(BASE_DAILY_REWARD * multiplier);

            await ctx.Set<Db.Models.DailyStreak>()
                .ToLinqToDBTable()
                .Where(x => x.UserId == userId)
                .UpdateAsync(_ => new Db.Models.DailyStreak
                {
                    CurrentStreak = newStreak,
                    LongestStreak = longest,
                    LastClaimUtc = now
                });

            await _cur.AddAsync(userId, reward, new("dailystreak", "claim"));
            return (newStreak, longest, reward, false);
        }

        // First time claiming
        var firstReward = BASE_DAILY_REWARD;

        await ctx.Set<Db.Models.DailyStreak>()
            .ToLinqToDBTable()
            .InsertAsync(() => new Db.Models.DailyStreak
            {
                UserId = userId,
                CurrentStreak = 1,
                LongestStreak = 1,
                LastClaimUtc = now
            });

        await _cur.AddAsync(userId, firstReward, new("dailystreak", "claim"));
        return (1, 1, firstReward, false);
    }

    public async Task<(int currentStreak, int longestStreak, DateTime lastClaim)> GetStreakAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var streak = await ctx.Set<Db.Models.DailyStreak>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (streak is null)
            return (0, 0, DateTime.MinValue);

        // Check if streak is still active (within 48h)
        var hoursSince = (DateTime.UtcNow - streak.LastClaimUtc).TotalHours;
        var currentStreak = hoursSince > 48 ? 0 : streak.CurrentStreak;

        return (currentStreak, streak.LongestStreak, streak.LastClaimUtc);
    }
}
