#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class FriendshipService : INService
{
    private readonly DbService _db;

    public FriendshipService(DbService db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Error)> SendRequestAsync(ulong guildId, ulong fromId, ulong toId)
    {
        if (fromId == toId)
            return (false, "You can't friend yourself!");

        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId &&
                ((x.User1Id == fromId && x.User2Id == toId) ||
                 (x.User1Id == toId && x.User2Id == fromId)))
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is not null)
        {
            if (existing.Accepted)
                return (false, "You're already friends!");
            return (false, "A friend request already exists!");
        }

        await ctx.GetTable<Friendship>()
            .InsertAsync(() => new Friendship
            {
                GuildId = guildId,
                User1Id = fromId,
                User2Id = toId,
                Accepted = false,
                InteractionCount = 0
            });

        return (true, null);
    }

    public async Task<(bool Success, string Error)> AcceptRequestAsync(ulong guildId, ulong userId, ulong fromId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.User1Id == fromId && x.User2Id == userId && !x.Accepted)
            .Set(x => x.Accepted, true)
            .UpdateAsync();

        return updated > 0 ? (true, null) : (false, "No pending request from that user!");
    }

    public async Task<(bool Success, string Error)> DenyRequestAsync(ulong guildId, ulong userId, ulong fromId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.User1Id == fromId && x.User2Id == userId && !x.Accepted)
            .DeleteAsync();

        return deleted > 0 ? (true, null) : (false, "No pending request from that user!");
    }

    public async Task<bool> RemoveFriendAsync(ulong guildId, ulong userId, ulong friendId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.Accepted &&
                ((x.User1Id == userId && x.User2Id == friendId) ||
                 (x.User1Id == friendId && x.User2Id == userId)))
            .DeleteAsync();
        return deleted > 0;
    }

    public async Task<List<Friendship>> GetFriendsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.Accepted &&
                (x.User1Id == userId || x.User2Id == userId))
            .ToListAsyncLinqToDB();
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.User2Id == userId && !x.Accepted)
            .ToListAsyncLinqToDB();
    }

    public async Task IncrementInteractionAsync(ulong guildId, ulong user1, ulong user2)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<Friendship>()
            .Where(x => x.GuildId == guildId && x.Accepted &&
                ((x.User1Id == user1 && x.User2Id == user2) ||
                 (x.User1Id == user2 && x.User2Id == user1)))
            .Set(x => x.InteractionCount, x => x.InteractionCount + 1)
            .UpdateAsync();
    }
}
