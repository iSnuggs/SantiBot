#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class XpBoosterService : INService, IReadyExecutor
{
    private readonly DbService _db;

    public XpBoosterService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await CleanExpiredBoostersAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task CleanExpiredBoostersAsync()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<XpBooster>()
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .DeleteAsync();
    }

    public async Task SetBoostAsync(ulong guildId, ulong userId, double multiplier, TimeSpan duration)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<XpBooster>()
            .InsertAsync(() => new XpBooster
            {
                GuildId = guildId,
                UserId = userId,
                Multiplier = multiplier,
                ExpiresAt = DateTime.UtcNow.Add(duration)
            });
    }

    public async Task<double> GetEffectiveMultiplierAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var boosters = await ctx.GetTable<XpBooster>()
            .Where(x => x.GuildId == guildId && x.ExpiresAt > DateTime.UtcNow &&
                         (x.UserId == userId || x.UserId == 0))
            .ToListAsyncLinqToDB();

        if (boosters.Count == 0) return 1.0;
        return boosters.Max(x => x.Multiplier);
    }

    public async Task<List<XpBooster>> GetActiveBoostersAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpBooster>()
            .Where(x => x.GuildId == guildId && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsyncLinqToDB();
    }
}
