#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Social;

public sealed class MarriageService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public MarriageService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<Marriage> GetMarriageAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Marriage>()
            .Where(x => x.GuildId == guildId && (x.User1Id == userId || x.User2Id == userId))
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<(bool Success, string Error)> MarryAsync(ulong guildId, ulong userId, ulong targetId)
    {
        if (userId == targetId)
            return (false, "You can't marry yourself!");

        var existing = await GetMarriageAsync(guildId, userId);
        if (existing is not null)
            return (false, "You're already married!");

        var targetExisting = await GetMarriageAsync(guildId, targetId);
        if (targetExisting is not null)
            return (false, "That person is already married!");

        var removed = await _cs.RemoveAsync(userId, 1000, new TxData("marriage", "Marriage Fee"));
        if (!removed)
            return (false, "You need 1000 \U0001F960 to get married!");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<Marriage>()
            .InsertAsync(() => new Marriage
            {
                GuildId = guildId,
                User1Id = userId,
                User2Id = targetId
            });

        return (true, null);
    }

    public async Task<(bool Success, string Error)> DivorceAsync(ulong guildId, ulong userId)
    {
        var existing = await GetMarriageAsync(guildId, userId);
        if (existing is null)
            return (false, "You're not married!");

        var removed = await _cs.RemoveAsync(userId, 500, new TxData("divorce", "Divorce Fee"));
        if (!removed)
            return (false, "You need 500 \U0001F960 to divorce!");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<Marriage>()
            .Where(x => x.Id == existing.Id)
            .DeleteAsync();

        return (true, null);
    }

    public async Task<(bool Success, string Error)> AdoptAsync(ulong guildId, ulong parentId, ulong childId)
    {
        if (parentId == childId)
            return (false, "You can't adopt yourself!");

        await using var ctx = _db.GetDbContext();

        var alreadyAdopted = await ctx.GetTable<Adoption>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.ChildId == childId);
        if (alreadyAdopted)
            return (false, "That user already has a parent!");

        // prevent circular adoption
        var isChild = await ctx.GetTable<Adoption>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.ParentId == childId && x.ChildId == parentId);
        if (isChild)
            return (false, "You can't adopt your own parent!");

        await ctx.GetTable<Adoption>()
            .InsertAsync(() => new Adoption
            {
                GuildId = guildId,
                ParentId = parentId,
                ChildId = childId
            });

        return (true, null);
    }

    public async Task<List<Adoption>> GetFamilyAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Adoption>()
            .Where(x => x.GuildId == guildId && (x.ParentId == userId || x.ChildId == userId))
            .ToListAsyncLinqToDB();
    }
}
