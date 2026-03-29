#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class UserNotesService : INService
{
    private readonly DbService _db;

    public UserNotesService(DbService db)
    {
        _db = db;
    }

    public async Task<UserNote> AddNoteAsync(ulong guildId, ulong userId, ulong modId, string note, string actionType = "note")
    {
        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<UserNote>()
            .InsertWithInt32IdentityAsync(() => new UserNote
            {
                GuildId = guildId,
                UserId = userId,
                ModeratorId = modId,
                Note = note,
                ActionType = actionType
            });

        return new UserNote { Id = id, UserId = userId, Note = note, ActionType = actionType };
    }

    public async Task<List<UserNote>> GetTimelineAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserNote>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .OrderByDescending(x => x.DateAdded)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteNoteAsync(ulong guildId, int noteId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserNote>()
            .Where(x => x.GuildId == guildId && x.Id == noteId)
            .DeleteAsync() > 0;
    }
}
