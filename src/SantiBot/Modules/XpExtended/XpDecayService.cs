#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class XpDecayService : INService, IReadyExecutor
{
    private readonly DbService _db;

    public XpDecayService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        // run decay check daily
        while (true)
        {
            var now = DateTime.UtcNow;
            var nextCheck = now.Date.AddDays(1).AddHours(3); // 3 AM UTC
            await Task.Delay(nextCheck - now);

            try
            {
                await ProcessDecayAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task ProcessDecayAsync()
    {
        await using var ctx = _db.GetDbContext();

        var configs = await ctx.GetTable<XpDecayConfig>()
            .Where(x => x.Enabled)
            .ToListAsyncLinqToDB();

        foreach (var config in configs)
        {
            var cutoff = DateTime.UtcNow.AddDays(-config.InactiveDays);

            // find users whose latest activity heatmap is older than cutoff
            var activeUsers = await ctx.GetTable<ActivityHeatmap>()
                .Where(x => x.GuildId == config.GuildId && x.Date >= cutoff.Date)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsyncLinqToDB();

            // get all users with XP in this guild
            var allXpUsers = await ctx.GetTable<UserXpStats>()
                .Where(x => x.GuildId == config.GuildId && x.Xp > 0)
                .ToListAsyncLinqToDB();

            foreach (var user in allXpUsers)
            {
                if (activeUsers.Contains(user.UserId)) continue;

                // decay XP but not below 0
                var newXp = Math.Max(0, user.Xp - config.XpLostPerDay);
                await ctx.GetTable<UserXpStats>()
                    .Where(x => x.GuildId == config.GuildId && x.UserId == user.UserId)
                    .Set(x => x.Xp, newXp)
                    .UpdateAsync();
            }
        }
    }

    public async Task<XpDecayConfig> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpDecayConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task SetEnabledAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<XpDecayConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<XpDecayConfig>()
                .InsertAsync(() => new XpDecayConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                    InactiveDays = 14,
                    XpLostPerDay = 50
                });
        }
        else
        {
            await ctx.GetTable<XpDecayConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, enabled)
                .UpdateAsync();
        }
    }

    public async Task SetRateAsync(ulong guildId, long xpPerDay, int inactiveDays)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<XpDecayConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<XpDecayConfig>()
                .InsertAsync(() => new XpDecayConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                    InactiveDays = inactiveDays,
                    XpLostPerDay = xpPerDay
                });
        }
        else
        {
            await ctx.GetTable<XpDecayConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.XpLostPerDay, xpPerDay)
                .Set(x => x.InactiveDays, inactiveDays)
                .UpdateAsync();
        }
    }
}
