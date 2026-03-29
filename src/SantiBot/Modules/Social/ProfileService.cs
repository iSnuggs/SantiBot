#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class ProfileService : INService
{
    private readonly DbService _db;

    public ProfileService(DbService db)
    {
        _db = db;
    }

    public async Task<UserProfile> GetOrCreateProfileAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var profile = await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .FirstOrDefaultAsyncLinqToDB();

        if (profile is null)
        {
            profile = new UserProfile
            {
                GuildId = guildId,
                UserId = userId,
                Bio = "",
                Title = "",
                Pronouns = "",
                Timezone = "",
                MessageCount = 0,
                BackgroundId = "default",
                BackgroundName = "Default",
                BackgroundColor = "#5865F2"
            };
            profile.Id = await ctx.GetTable<UserProfile>()
                .InsertWithInt32IdentityAsync(() => new UserProfile
                {
                    GuildId = guildId,
                    UserId = userId,
                    Bio = "",
                    Title = "",
                    Pronouns = "",
                    Timezone = "",
                    MessageCount = 0,
                    BackgroundId = "default",
                    BackgroundName = "Default",
                    BackgroundColor = "#5865F2"
                });
        }

        return profile;
    }

    public async Task SetBioAsync(ulong guildId, ulong userId, string bio)
    {
        await using var ctx = _db.GetDbContext();
        var profile = await GetOrCreateProfileAsync(guildId, userId);
        await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.Bio, bio)
            .UpdateAsync();
    }

    public async Task SetTitleAsync(ulong guildId, ulong userId, string title)
    {
        await using var ctx = _db.GetDbContext();
        await GetOrCreateProfileAsync(guildId, userId);
        await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.Title, title)
            .UpdateAsync();
    }

    public async Task SetPronounsAsync(ulong guildId, ulong userId, string pronouns)
    {
        await using var ctx = _db.GetDbContext();
        await GetOrCreateProfileAsync(guildId, userId);
        await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.Pronouns, pronouns)
            .UpdateAsync();
    }

    public async Task SetTimezoneAsync(ulong guildId, ulong userId, string tz)
    {
        await using var ctx = _db.GetDbContext();
        await GetOrCreateProfileAsync(guildId, userId);
        await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.Timezone, tz)
            .UpdateAsync();
    }

    public async Task IncrementMessageCountAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var updated = await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.MessageCount, x => x.MessageCount + 1)
            .UpdateAsync();

        if (updated == 0)
        {
            await GetOrCreateProfileAsync(guildId, userId);
            await ctx.GetTable<UserProfile>()
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .Set(x => x.MessageCount, 1)
                .UpdateAsync();
        }
    }

    public async Task SetBackgroundAsync(ulong guildId, ulong userId, string bgId, string name, string color)
    {
        await using var ctx = _db.GetDbContext();
        await GetOrCreateProfileAsync(guildId, userId);
        await ctx.GetTable<UserProfile>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Set(x => x.BackgroundId, bgId)
            .Set(x => x.BackgroundName, name)
            .Set(x => x.BackgroundColor, color)
            .UpdateAsync();
    }
}
