#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class LevelUpMessageService : INService
{
    private readonly DbService _db;

    public LevelUpMessageService(DbService db)
    {
        _db = db;
    }

    public async Task<LevelUpMessage> GetConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<LevelUpMessage>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task SetMessageAsync(ulong guildId, string template)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<LevelUpMessage>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<LevelUpMessage>()
                .InsertAsync(() => new LevelUpMessage
                {
                    GuildId = guildId,
                    MessageTemplate = template,
                    ChannelId = 0,
                    Enabled = true
                });
        }
        else
        {
            await ctx.GetTable<LevelUpMessage>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.MessageTemplate, template)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
        }
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<LevelUpMessage>()
            .Where(x => x.GuildId == guildId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is null)
        {
            await ctx.GetTable<LevelUpMessage>()
                .InsertAsync(() => new LevelUpMessage
                {
                    GuildId = guildId,
                    MessageTemplate = "\U0001F389 {user} reached level {level}!",
                    ChannelId = channelId,
                    Enabled = true
                });
        }
        else
        {
            await ctx.GetTable<LevelUpMessage>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.ChannelId, channelId)
                .UpdateAsync();
        }
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<LevelUpMessage>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();
    }

    public string FormatMessage(string template, string userName, int level, string guildName)
    {
        return template
            .Replace("{user}", userName)
            .Replace("{level}", level.ToString())
            .Replace("{guild}", guildName);
    }
}
