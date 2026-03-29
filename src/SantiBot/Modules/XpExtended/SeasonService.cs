#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.XpExtended;

public sealed class SeasonService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    // season rewards: level -> (cookie reward, title)
    private static readonly Dictionary<int, (long Cookies, string Title)> _rewards = new()
    {
        { 5, (500, "Season Rookie") },
        { 10, (1000, "Season Regular") },
        { 15, (2000, "Season Veteran") },
        { 20, (3000, "Season Elite") },
        { 25, (5000, "Season Champion") },
        { 30, (7500, "Season Legend") },
        { 35, (10000, "Season Master") },
        { 40, (15000, "Season Grandmaster") },
        { 45, (20000, "Season Mythic") },
        { 50, (50000, "Season Immortal") },
    };

    public SeasonService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<SeasonConfig> GetActiveSeasonAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SeasonConfig>()
            .Where(x => x.GuildId == guildId && x.Active && x.EndDate > DateTime.UtcNow)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<SeasonConfig> CreateSeasonAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // deactivate existing
        await ctx.GetTable<SeasonConfig>()
            .Where(x => x.GuildId == guildId && x.Active)
            .Set(x => x.Active, false)
            .UpdateAsync();

        var lastSeason = await ctx.GetTable<SeasonConfig>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.SeasonNumber)
            .FirstOrDefaultAsyncLinqToDB();

        var newNumber = (lastSeason?.SeasonNumber ?? 0) + 1;

        var id = await ctx.GetTable<SeasonConfig>()
            .InsertWithInt32IdentityAsync(() => new SeasonConfig
            {
                GuildId = guildId,
                SeasonNumber = newNumber,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(90),
                Active = true
            });

        return new SeasonConfig
        {
            Id = id,
            GuildId = guildId,
            SeasonNumber = newNumber,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(90),
            Active = true
        };
    }

    public async Task<SeasonProgress> GetProgressAsync(ulong guildId, ulong userId, int seasonNumber)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SeasonProgress>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.SeasonNumber == seasonNumber)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task AddSeasonXpAsync(ulong guildId, ulong userId, int seasonNumber, long xp)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<SeasonProgress>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && x.SeasonNumber == seasonNumber)
            .Set(x => x.SeasonXp, x => x.SeasonXp + xp)
            .UpdateAsync();

        if (updated == 0)
        {
            await ctx.GetTable<SeasonProgress>()
                .InsertAsync(() => new SeasonProgress
                {
                    GuildId = guildId,
                    UserId = userId,
                    SeasonNumber = seasonNumber,
                    SeasonXp = xp,
                    ClaimedLevel = 0
                });
        }
    }

    public int GetSeasonLevel(long xp)
    {
        // 1000 XP per season level
        return (int)Math.Min(50, xp / 1000);
    }

    public Dictionary<int, (long Cookies, string Title)> GetRewards() => _rewards;

    public async Task<(bool Success, string Error, long Reward)> ClaimRewardAsync(ulong guildId, ulong userId, int seasonNumber, int level)
    {
        if (!_rewards.ContainsKey(level))
            return (false, $"No reward at level {level}!", 0);

        var progress = await GetProgressAsync(guildId, userId, seasonNumber);
        if (progress is null)
            return (false, "No season progress found!", 0);

        var currentLevel = GetSeasonLevel(progress.SeasonXp);
        if (currentLevel < level)
            return (false, $"You need season level {level} (currently {currentLevel})!", 0);

        if (progress.ClaimedLevel >= level)
            return (false, "You already claimed this reward!", 0);

        var reward = _rewards[level];

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<SeasonProgress>()
            .Where(x => x.Id == progress.Id)
            .Set(x => x.ClaimedLevel, level)
            .UpdateAsync();

        await _cs.AddAsync(userId, reward.Cookies, new TxData("season", $"Season Reward L{level}"));

        return (true, null, reward.Cookies);
    }
}
