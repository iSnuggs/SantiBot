#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class MoodService : INService, IReadyExecutor
{
    private readonly DbService _db;

    public MoodService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await CleanExpiredMoodsAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task CleanExpiredMoodsAsync()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<UserMood>()
            .Where(x => x.ExpiresAt < DateTime.UtcNow)
            .DeleteAsync();
    }

    public async Task SetMoodAsync(ulong guildId, ulong userId, string emoji, string message)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<UserMood>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (existing is not null)
        {
            await ctx.GetTable<UserMood>()
                .Where(x => x.Id == existing.Id)
                .Set(x => x.Emoji, emoji)
                .Set(x => x.Message, message)
                .Set(x => x.ExpiresAt, DateTime.UtcNow.AddHours(24))
                .UpdateAsync();
        }
        else
        {
            await ctx.GetTable<UserMood>()
                .InsertAsync(() => new UserMood
                {
                    GuildId = guildId,
                    UserId = userId,
                    Emoji = emoji,
                    Message = message,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                });
        }
    }

    public async Task ClearMoodAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<UserMood>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .DeleteAsync();
    }

    public async Task<List<UserMood>> GetMoodboardAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserMood>()
            .Where(x => x.GuildId == guildId && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsyncLinqToDB();
    }
}
