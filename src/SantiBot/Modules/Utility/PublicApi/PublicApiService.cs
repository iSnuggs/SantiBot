#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Security.Cryptography;

namespace SantiBot.Modules.Utility.PublicApi;

/// <summary>
/// Public REST API service.
/// API endpoints (require API key auth via X-Api-Key header):
///   GET /api/public/v1/guild/{guildId}/leaderboard
///   GET /api/public/v1/guild/{guildId}/stats
///   GET /api/public/v1/guild/{guildId}/user/{userId}
/// </summary>
public sealed class PublicApiService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public PublicApiService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<string> GenerateApiKeyAsync(ulong guildId, ulong userId)
    {
        var key = $"santi_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";

        await using var uow = _db.GetDbContext();
        // Revoke existing keys for this user in this guild
        await uow.GetTable<ApiKey>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && !x.IsRevoked)
            .UpdateAsync(x => new ApiKey { IsRevoked = true });

        await uow.GetTable<ApiKey>()
            .InsertAsync(() => new ApiKey
            {
                GuildId = guildId,
                UserId = userId,
                Key = key,
                IsRevoked = false
            });

        return key;
    }

    public async Task<bool> RevokeApiKeyAsync(ulong guildId, ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<ApiKey>()
            .Where(x => x.GuildId == guildId && x.UserId == userId && !x.IsRevoked)
            .UpdateAsync(x => new ApiKey { IsRevoked = true });
        return count > 0;
    }

    public async Task<bool> ValidateKeyAsync(string key)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<ApiKey>()
            .AnyAsyncLinqToDB(x => x.Key == key && !x.IsRevoked);
    }

    public record LeaderboardEntry(ulong UserId, string Username, long TotalXp, long CurrencyAmount);

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(ulong guildId, int count = 20)
    {
        await using var uow = _db.GetDbContext();
        var entries = await uow.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Xp)
            .Take(count)
            .ToListAsyncLinqToDB();

        var result = new List<LeaderboardEntry>();
        foreach (var e in entries)
        {
            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(e.UserId);
            var discordUser = await uow.GetTable<DiscordUser>()
                .FirstOrDefaultAsyncLinqToDB(x => x.UserId == e.UserId);

            result.Add(new LeaderboardEntry(
                e.UserId,
                user?.Username ?? discordUser?.Username ?? "Unknown",
                e.Xp,
                discordUser?.CurrencyAmount ?? 0));
        }

        return result;
    }

    public record GuildStats(
        ulong GuildId,
        string Name,
        int MemberCount,
        int OnlineCount,
        int ChannelCount,
        int RoleCount,
        DateTime? CreatedAt);

    public GuildStats GetGuildStats(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return null;

        return new GuildStats(
            guild.Id,
            guild.Name,
            guild.MemberCount,
            guild.Users.Count(u => u.Status != UserStatus.Offline),
            guild.Channels.Count,
            guild.Roles.Count,
            guild.CreatedAt.UtcDateTime);
    }

    public record UserStats(
        ulong UserId,
        string Username,
        long Xp,
        long Currency,
        int Level,
        List<string> Roles);

    public async Task<UserStats> GetUserStatsAsync(ulong guildId, ulong userId)
    {
        var guild = _client.GetGuild(guildId);
        var guildUser = guild?.GetUser(userId);
        if (guildUser is null) return null;

        await using var uow = _db.GetDbContext();
        var xpStats = await uow.GetTable<UserXpStats>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);
        var discordUser = await uow.GetTable<DiscordUser>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId);

        var xp = xpStats?.Xp ?? 0;
        // Simple level calc: level = sqrt(xp / 100)
        var level = (int)Math.Floor(Math.Sqrt(xp / 100.0));

        return new UserStats(
            userId,
            guildUser.Username,
            xp,
            discordUser?.CurrencyAmount ?? 0,
            level,
            guildUser.Roles.Select(r => r.Name).ToList());
    }
}
