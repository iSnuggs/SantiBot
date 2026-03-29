#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.Seasons;

public sealed class SeasonService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    private static readonly long[] TopRewards = [5000, 3000, 2000, 1500, 1000, 800, 600, 400, 300, 200];

    public SeasonService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<EconomySeason> GetActiveSeasonAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var season = await ctx.GetTable<EconomySeason>()
            .FirstOrDefaultAsyncLinqToDB(s => s.GuildId == guildId && s.IsActive);

        if (season is null)
        {
            var maxNum = await ctx.GetTable<EconomySeason>()
                .Where(s => s.GuildId == guildId)
                .Select(s => (int?)s.SeasonNumber)
                .MaxAsyncLinqToDB() ?? 0;

            await ctx.GetTable<EconomySeason>().InsertAsync(() => new EconomySeason
            {
                GuildId = guildId,
                SeasonNumber = maxNum + 1,
                StartedAt = DateTime.UtcNow,
                IsActive = true,
                DateAdded = DateTime.UtcNow
            });

            season = await ctx.GetTable<EconomySeason>()
                .FirstOrDefaultAsyncLinqToDB(s => s.GuildId == guildId && s.IsActive);
        }

        return season;
    }

    public async Task RecordEarningsAsync(ulong guildId, ulong userId, long amount)
    {
        var season = await GetActiveSeasonAsync(guildId);
        await using var ctx = _db.GetDbContext();
        var entry = await ctx.GetTable<SeasonEarnings>()
            .FirstOrDefaultAsyncLinqToDB(e => e.GuildId == guildId && e.UserId == userId && e.SeasonNumber == season.SeasonNumber);

        if (entry is null)
        {
            await ctx.GetTable<SeasonEarnings>().InsertAsync(() => new SeasonEarnings
            {
                GuildId = guildId,
                UserId = userId,
                SeasonNumber = season.SeasonNumber,
                TotalEarned = amount,
                DateAdded = DateTime.UtcNow
            });
        }
        else
        {
            await ctx.GetTable<SeasonEarnings>()
                .Where(e => e.Id == entry.Id)
                .UpdateAsync(e => new SeasonEarnings { TotalEarned = entry.TotalEarned + amount });
        }
    }

    public async Task<List<SeasonEarnings>> GetLeaderboardAsync(ulong guildId)
    {
        var season = await GetActiveSeasonAsync(guildId);
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<SeasonEarnings>()
            .Where(e => e.GuildId == guildId && e.SeasonNumber == season.SeasonNumber)
            .OrderByDescending(e => e.TotalEarned)
            .Take(10)
            .ToListAsyncLinqToDB();
    }

    public async Task<(bool Success, string Message)> ResetSeasonAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var season = await ctx.GetTable<EconomySeason>()
            .FirstOrDefaultAsyncLinqToDB(s => s.GuildId == guildId && s.IsActive);

        if (season is null)
            return (false, "No active season!");

        // Get top 10 and reward them
        var top = await ctx.GetTable<SeasonEarnings>()
            .Where(e => e.GuildId == guildId && e.SeasonNumber == season.SeasonNumber)
            .OrderByDescending(e => e.TotalEarned)
            .Take(10)
            .ToListAsyncLinqToDB();

        for (int i = 0; i < top.Count && i < TopRewards.Length; i++)
            await _cs.AddAsync(top[i].UserId, TopRewards[i], new TxData("season", "reward"));

        // End current season
        await ctx.GetTable<EconomySeason>()
            .Where(s => s.Id == season.Id)
            .UpdateAsync(s => new EconomySeason { IsActive = false, EndedAt = DateTime.UtcNow });

        // Start new one
        await ctx.GetTable<EconomySeason>().InsertAsync(() => new EconomySeason
        {
            GuildId = guildId,
            SeasonNumber = season.SeasonNumber + 1,
            StartedAt = DateTime.UtcNow,
            IsActive = true,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"Season {season.SeasonNumber} ended! Top earners rewarded. Season {season.SeasonNumber + 1} started!");
    }

    public async Task<List<EconomySeason>> GetHistoryAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<EconomySeason>()
            .Where(s => s.GuildId == guildId && !s.IsActive)
            .OrderByDescending(s => s.SeasonNumber)
            .Take(10)
            .ToListAsyncLinqToDB();
    }
}
