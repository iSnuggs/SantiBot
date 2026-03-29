#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class EvidenceLockerService : INService
{
    private readonly DbService _db;

    public EvidenceLockerService(DbService db)
    {
        _db = db;
    }

    public async Task<EvidenceItem> AddEvidenceAsync(ulong guildId, int caseId, string url, string note, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<EvidenceItem>()
            .InsertWithInt32IdentityAsync(() => new EvidenceItem
            {
                GuildId = guildId,
                CaseId = caseId,
                Url = url,
                Note = note ?? "",
                AddedByUserId = userId
            });

        return new EvidenceItem { Id = id, CaseId = caseId, Url = url, Note = note };
    }

    public async Task<List<EvidenceItem>> ListEvidenceAsync(ulong guildId, int caseId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<EvidenceItem>()
            .Where(x => x.GuildId == guildId && x.CaseId == caseId)
            .OrderByDescending(x => x.DateAdded)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> RemoveEvidenceAsync(ulong guildId, int evidenceId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<EvidenceItem>()
            .Where(x => x.GuildId == guildId && x.Id == evidenceId)
            .DeleteAsync() > 0;
    }
}
