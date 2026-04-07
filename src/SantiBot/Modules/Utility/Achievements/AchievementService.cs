#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility.Achievements;

public sealed class AchievementService(DbService _db) : INService
{
    public record AchievementDef(string Id, string Name, string Description, string Emoji, string Category);

    public static readonly AchievementDef[] AllAchievements =
    [
        // Messages
        new("msg_100", "Chatterbox", "Send 100 messages", "💬", "Messages"),
        new("msg_1000", "Motormouth", "Send 1,000 messages", "🗣️", "Messages"),
        new("msg_10000", "Legend of Chat", "Send 10,000 messages", "📜", "Messages"),

        // XP
        new("xp_level_5", "Getting Started", "Reach XP level 5", "⭐", "XP"),
        new("xp_level_10", "Rising Star", "Reach XP level 10", "🌟", "XP"),
        new("xp_level_25", "Veteran", "Reach XP level 25", "💫", "XP"),
        new("xp_level_50", "Elite", "Reach XP level 50", "👑", "XP"),

        // Economy
        new("eco_10k", "Making Bank", "Earn 10,000 currency", "💰", "Economy"),
        new("eco_100k", "Rich Kid", "Earn 100,000 currency", "💎", "Economy"),
        new("eco_1m", "Millionaire", "Earn 1,000,000 currency", "🏆", "Economy"),

        // Social
        new("rep_10", "Beloved", "Receive 10 reputation points", "❤️", "Social"),
        new("giveaway_5", "Lucky Winner", "Win 5 giveaways", "🎉", "Social"),

        // Fishing
        new("fish_50", "Angler", "Catch 50 fish", "🐟", "Fishing"),
        new("fish_legendary", "Legendary Catch", "Catch a legendary fish", "🐉", "Fishing"),

        // Streaks
        new("streak_7", "Consistent", "Maintain a 7-day daily streak", "🔥", "Streaks"),
        new("streak_30", "Dedicated", "Maintain a 30-day daily streak", "🔥", "Streaks"),
        new("streak_365", "Unstoppable", "Maintain a 365-day daily streak", "☄️", "Streaks"),
    ];

    /// <summary>
    /// Try to unlock an achievement for a user. Returns true if newly unlocked.
    /// </summary>
    public async Task<bool> TryUnlockAsync(ulong userId, string achievementId)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<UserAchievement>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AchievementId == achievementId);

        if (existing is not null)
            return false;

        ctx.Set<UserAchievement>().Add(new UserAchievement
        {
            UserId = userId,
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get all unlocked achievements for a user.
    /// </summary>
    public async Task<List<UserAchievement>> GetUserAchievementsAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserAchievement>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UnlockedAt)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Get the achievement definition by id, or null if not found.
    /// </summary>
    public static AchievementDef GetDef(string id)
        => AllAchievements.FirstOrDefault(a => a.Id == id);

    /// <summary>
    /// Get all achievement definitions grouped by category.
    /// </summary>
    public static IEnumerable<IGrouping<string, AchievementDef>> GetAllGrouped()
        => AllAchievements.GroupBy(a => a.Category);

    /// <summary>
    /// Get top N users in a guild by number of achievements unlocked.
    /// </summary>
    public async Task<List<(ulong UserId, int Count)>> GetLeaderboardAsync(ulong guildId, int limit = 10)
    {
        await using var ctx = _db.GetDbContext();

        // Get all users who have achievements, join with DiscordUser to filter by guild
        var guildUserIds = await ctx.GetTable<UserXpStats>()
            .Where(x => x.GuildId == guildId)
            .Select(x => x.UserId)
            .ToListAsyncLinqToDB();

        if (guildUserIds.Count == 0)
            return [];

        return await ctx.GetTable<UserAchievement>()
            .Where(x => guildUserIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.UserId, x.Count)).ToList());
    }
}
