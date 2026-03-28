using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class ReputationService : INService
{
    private readonly DbService _db;

    public ReputationService(DbService db)
    {
        _db = db;
    }

    /// <summary>
    /// Give a rep point to another user. Returns null on success,
    /// or a TimeSpan indicating how long until they can rep again.
    /// </summary>
    public async Task<TimeSpan?> GiveRepAsync(ulong guildId, ulong giverId, ulong receiverId)
    {
        if (giverId == receiverId)
            return TimeSpan.Zero; // sentinel: can't rep yourself

        await using var ctx = _db.GetDbContext();

        // Check 24h cooldown
        var lastRep = await ctx
            .GetTable<RepLog>()
            .Where(x => x.GuildId == guildId && x.GiverUserId == giverId)
            .OrderByDescending(x => x.GivenAt)
            .FirstOrDefaultAsyncLinqToDB();

        if (lastRep is not null)
        {
            var cooldownEnd = lastRep.GivenAt.AddHours(24);
            if (cooldownEnd > DateTime.UtcNow)
                return cooldownEnd - DateTime.UtcNow;
        }

        // Upsert the receiver's rep count
        var existing = await ctx
            .GetTable<UserReputation>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == receiverId);

        if (existing is null)
        {
            await ctx.GetTable<UserReputation>()
                .InsertAsync(() => new UserReputation
                {
                    GuildId = guildId,
                    UserId = receiverId,
                    RepCount = 1,
                });
        }
        else
        {
            await ctx.GetTable<UserReputation>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.RepCount, x => x.RepCount + 1)
                .UpdateAsync();
        }

        // Log the rep
        await ctx.GetTable<RepLog>()
            .InsertAsync(() => new RepLog
            {
                GuildId = guildId,
                GiverUserId = giverId,
                ReceiverUserId = receiverId,
                GivenAt = DateTime.UtcNow,
            });

        return null;
    }

    public async Task<int> GetRepAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var rep = await ctx.GetTable<UserReputation>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        return rep?.RepCount ?? 0;
    }

    public async Task<IReadOnlyList<UserReputation>> GetLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx.GetTable<UserReputation>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.RepCount)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
