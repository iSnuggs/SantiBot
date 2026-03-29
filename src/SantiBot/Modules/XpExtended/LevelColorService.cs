#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class LevelColorService : INService
{
    private readonly DbService _db;

    public LevelColorService(DbService db)
    {
        _db = db;
    }

    public async Task<LevelColorConfig> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<LevelColorConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task SetEnabledAsync(ulong guildId, bool enabled)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<LevelColorConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<LevelColorConfig>()
                .InsertAsync(() => new LevelColorConfig
                {
                    GuildId = guildId,
                    Enabled = enabled,
                    StartColor = "#3498DB",
                    EndColor = "#E74C3C"
                });
        }
        else
        {
            await ctx.GetTable<LevelColorConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.Enabled, enabled)
                .UpdateAsync();
        }
    }

    public async Task SetColorsAsync(ulong guildId, string startHex, string endHex)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<LevelColorConfig>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<LevelColorConfig>()
                .InsertAsync(() => new LevelColorConfig
                {
                    GuildId = guildId,
                    Enabled = true,
                    StartColor = startHex,
                    EndColor = endHex
                });
        }
        else
        {
            await ctx.GetTable<LevelColorConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.StartColor, startHex)
                .Set(x => x.EndColor, endHex)
                .UpdateAsync();
        }
    }

    public static Discord.Color InterpolateColor(string startHex, string endHex, int level, int maxLevel = 100)
    {
        var t = Math.Clamp((double)level / maxLevel, 0.0, 1.0);

        var sr = Convert.ToByte(startHex.Substring(1, 2), 16);
        var sg = Convert.ToByte(startHex.Substring(3, 2), 16);
        var sb = Convert.ToByte(startHex.Substring(5, 2), 16);

        var er = Convert.ToByte(endHex.Substring(1, 2), 16);
        var eg = Convert.ToByte(endHex.Substring(3, 2), 16);
        var eb = Convert.ToByte(endHex.Substring(5, 2), 16);

        var r = (byte)(sr + (er - sr) * t);
        var g = (byte)(sg + (eg - sg) * t);
        var b = (byte)(sb + (eb - sb) * t);

        return new Discord.Color(r, g, b);
    }
}
