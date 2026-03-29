#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class TeamXpService : INService
{
    private readonly DbService _db;

    public TeamXpService(DbService db)
    {
        _db = db;
    }

    public async Task<(bool Success, string Error)> CreateTeamAsync(ulong guildId, ulong ownerId, string name)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<XpTeam>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .AnyAsyncLinqToDB();
        if (existing)
            return (false, "A team with that name already exists!");

        var alreadyInTeam = await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.UserId == ownerId)
            .AnyAsyncLinqToDB();
        if (alreadyInTeam)
            return (false, "You're already in a team! Leave first.");

        var teamId = await ctx.GetTable<XpTeam>()
            .InsertWithInt32IdentityAsync(() => new XpTeam
            {
                GuildId = guildId,
                Name = name,
                OwnerId = ownerId,
                TotalXp = 0
            });

        await ctx.GetTable<XpTeamMember>()
            .InsertAsync(() => new XpTeamMember
            {
                GuildId = guildId,
                UserId = ownerId,
                TeamId = teamId
            });

        return (true, null);
    }

    public async Task<(bool Success, string Error)> JoinTeamAsync(ulong guildId, ulong userId, string name)
    {
        await using var ctx = _db.GetDbContext();

        var team = await ctx.GetTable<XpTeam>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .FirstOrDefaultAsyncLinqToDB();
        if (team is null)
            return (false, "Team not found!");

        var alreadyInTeam = await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .AnyAsyncLinqToDB();
        if (alreadyInTeam)
            return (false, "You're already in a team!");

        await ctx.GetTable<XpTeamMember>()
            .InsertAsync(() => new XpTeamMember
            {
                GuildId = guildId,
                UserId = userId,
                TeamId = team.Id
            });

        return (true, null);
    }

    public async Task<(bool Success, string Error)> LeaveTeamAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var membership = await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();
        if (membership is null)
            return (false, "You're not in a team!");

        // check if owner
        var team = await ctx.GetTable<XpTeam>()
            .Where(x => x.Id == membership.TeamId)
            .FirstOrDefaultAsyncLinqToDB();
        if (team is not null && team.OwnerId == userId)
            return (false, "Team owners can't leave! Transfer ownership first or disband.");

        await ctx.GetTable<XpTeamMember>()
            .Where(x => x.Id == membership.Id)
            .DeleteAsync();

        return (true, null);
    }

    public async Task AddTeamXpAsync(ulong guildId, ulong userId, long xp)
    {
        await using var ctx = _db.GetDbContext();
        var membership = await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (membership is null) return;

        await ctx.GetTable<XpTeam>()
            .Where(x => x.Id == membership.TeamId)
            .Set(x => x.TotalXp, x => x.TotalXp + xp)
            .UpdateAsync();
    }

    public async Task<XpTeam> GetUserTeamAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var membership = await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();
        if (membership is null) return null;

        return await ctx.GetTable<XpTeam>()
            .Where(x => x.Id == membership.TeamId)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<XpTeam> GetTeamByNameAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpTeam>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .FirstOrDefaultAsyncLinqToDB();
    }

    public async Task<List<XpTeamMember>> GetTeamMembersAsync(ulong guildId, int teamId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpTeamMember>()
            .Where(x => x.GuildId == guildId && x.TeamId == teamId)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<XpTeam>> GetLeaderboardAsync(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<XpTeam>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.TotalXp)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
