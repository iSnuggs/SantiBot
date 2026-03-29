#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ModTemplatesService : INService
{
    private readonly DbService _db;

    public ModTemplatesService(DbService db)
    {
        _db = db;
    }

    public async Task<bool> AddTemplateAsync(ulong guildId, string name, string reason)
    {
        await using var ctx = _db.GetDbContext();

        var exists = await ctx.GetTable<ModActionTemplate>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);

        if (exists) return false;

        await ctx.GetTable<ModActionTemplate>()
            .InsertAsync(() => new ModActionTemplate
            {
                GuildId = guildId,
                Name = name,
                Reason = reason
            });

        return true;
    }

    public async Task<bool> DeleteTemplateAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModActionTemplate>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync() > 0;
    }

    public async Task<List<ModActionTemplate>> ListTemplatesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModActionTemplate>()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Name)
            .ToListAsyncLinqToDB();
    }

    public async Task<ModActionTemplate> GetTemplateAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModActionTemplate>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);
    }
}
