#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class AchievementService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public static readonly List<(string Id, string Name, string Desc, string Emoji)> AllAchievements = new()
    {
        ("first_msg", "First Message", "Send your first message", "\U0001F4AC"),
        ("msg_100", "Chatterbox", "Send 100 messages", "\U0001F4E3"),
        ("msg_1000", "Keyboard Warrior", "Send 1,000 messages", "\u2328\uFE0F"),
        ("msg_10000", "Legendary Talker", "Send 10,000 messages", "\U0001F3C6"),
        ("night_owl", "Night Owl", "Send a message after midnight", "\U0001F989"),
        ("early_bird", "Early Bird", "Send a message before 6 AM", "\U0001F426"),
        ("weekend_warrior", "Weekend Warrior", "Be active on a weekend", "\U0001F3AE"),
        ("streak_7", "7-Day Streak", "Be active 7 days in a row", "\U0001F525"),
        ("karma_10", "Well Liked", "Reach 10 karma", "\u2B50"),
        ("karma_100", "Community Favorite", "Reach 100 karma", "\U0001F31F"),
        ("married", "Taken", "Get married", "\U0001F48D"),
        ("voice_1h", "Voice Regular", "Spend 1 hour in voice", "\U0001F3A4"),
        ("voice_24h", "Voice Addict", "Spend 24 hours in voice", "\U0001F399\uFE0F"),
        ("friends_5", "Social Butterfly", "Have 5 friends", "\U0001F98B"),
        ("prestige_1", "Prestige I", "Prestige for the first time", "\u2B50"),
    };

    public AchievementService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        _client.MessageReceived += OnMessageReceived;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage msg)
    {
        try
        {
            if (msg.Author.IsBot) return;
            if (msg.Channel is not ITextChannel tc) return;

            var guildId = tc.GuildId;
            var userId = msg.Author.Id;
            var hour = DateTime.UtcNow.Hour;

            // check message-based achievements
            await using var ctx = _db.GetDbContext();
            var profile = await ctx.GetTable<UserProfile>()
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .FirstOrDefaultAsyncLinqToDB();

            if (profile is null) return;

            var msgCount = profile.MessageCount;

            if (msgCount >= 1)
                await TryAwardAsync(guildId, userId, "first_msg");
            if (msgCount >= 100)
                await TryAwardAsync(guildId, userId, "msg_100");
            if (msgCount >= 1000)
                await TryAwardAsync(guildId, userId, "msg_1000");
            if (msgCount >= 10000)
                await TryAwardAsync(guildId, userId, "msg_10000");
            if (hour >= 0 && hour < 4)
                await TryAwardAsync(guildId, userId, "night_owl");
            if (hour >= 4 && hour < 6)
                await TryAwardAsync(guildId, userId, "early_bird");
            if (DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                await TryAwardAsync(guildId, userId, "weekend_warrior");
        }
        catch { /* ignore */ }
    }

    public async Task TryAwardAsync(ulong guildId, ulong userId, string achievementId)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<UserAchievement>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId && x.AchievementId == achievementId);

        if (existing) return;

        var def = AllAchievements.FirstOrDefault(x => x.Id == achievementId);
        if (def == default) return;

        await ctx.GetTable<UserAchievement>()
            .InsertAsync(() => new UserAchievement
            {
                GuildId = guildId,
                UserId = userId,
                AchievementId = achievementId,
                AchievementName = def.Name,
                Description = def.Desc,
                Emoji = def.Emoji
            });
    }

    public async Task<List<UserAchievement>> GetAchievementsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserAchievement>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .ToListAsyncLinqToDB();
    }
}
