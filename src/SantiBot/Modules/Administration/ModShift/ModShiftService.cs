#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration.Services;

public sealed class ModShiftService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public ModShiftService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<ModShift> AddShiftAsync(ulong guildId, ulong userId, DayOfWeek day, int startHour, int endHour)
    {
        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<ModShift>()
            .InsertWithInt32IdentityAsync(() => new ModShift
            {
                GuildId = guildId,
                UserId = userId,
                DayOfWeek = day,
                StartHour = startHour,
                EndHour = endHour
            });

        return new ModShift { Id = id, GuildId = guildId, UserId = userId, DayOfWeek = day, StartHour = startHour, EndHour = endHour };
    }

    public async Task<bool> RemoveShiftAsync(ulong guildId, int shiftId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModShift>()
            .Where(x => x.GuildId == guildId && x.Id == shiftId)
            .DeleteAsync() > 0;
    }

    public async Task<List<ModShift>> ListShiftsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModShift>()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartHour)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<ModShift>> GetOnDutyAsync(ulong guildId)
    {
        var now = DateTime.UtcNow;
        var day = now.DayOfWeek;
        var hour = now.Hour;

        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ModShift>()
            .Where(x => x.GuildId == guildId && x.DayOfWeek == day)
            .Where(x => (x.StartHour <= x.EndHour && hour >= x.StartHour && hour < x.EndHour)
                     || (x.StartHour > x.EndHour && (hour >= x.StartHour || hour < x.EndHour)))
            .ToListAsyncLinqToDB();
    }
}
