#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class PrestigeService : INService
{
    private readonly DbService _db;

    // XP needed for a given level: level * level * 100
    private const int PRESTIGE_MIN_LEVEL = 50;

    public PrestigeService(DbService db)
    {
        _db = db;
    }

    public static long XpForLevel(int level) => (long)level * level * 100;
    public static int LevelFromXp(long xp)
    {
        if (xp <= 0) return 0;
        return (int)Math.Floor(Math.Sqrt(xp / 100.0));
    }

    public async Task<UserPrestige> GetPrestigeAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserPrestige>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<(bool Success, string Error, int NewPrestige)> PrestigeAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var xpStats = await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (xpStats is null)
            return (false, "You have no XP in this server!", 0);

        var level = LevelFromXp(xpStats.Xp);
        if (level < PRESTIGE_MIN_LEVEL)
            return (false, $"You need to be level {PRESTIGE_MIN_LEVEL}+ to prestige! (Currently level {level})", 0);

        var existing = await ctx.GetTable<UserPrestige>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        int newPrestige;
        if (existing is null)
        {
            newPrestige = 1;
            await ctx.GetTable<UserPrestige>()
                .InsertAsync(() => new UserPrestige
                {
                    GuildId = guildId,
                    UserId = userId,
                    PrestigeLevel = 1,
                    LastPrestigeDate = DateTime.UtcNow
                });
        }
        else
        {
            newPrestige = existing.PrestigeLevel + 1;
            await ctx.GetTable<UserPrestige>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.PrestigeLevel, newPrestige)
                .Set(x => x.LastPrestigeDate, DateTime.UtcNow)
                .UpdateAsync();
        }

        // reset XP to 0
        await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.Xp, 0)
            .UpdateAsync();

        return (true, null, newPrestige);
    }

    public double GetXpMultiplier(int prestigeLevel) => 1.0 + (prestigeLevel * 0.1);

    public async Task<List<UserPrestige>> GetLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserPrestige>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.PrestigeLevel)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    public static string GetPrestigeStars(int level)
    {
        if (level <= 0) return "";
        if (level <= 5) return new string('\u2B50', level);
        return $"\u2B50 x{level}";
    }
}
