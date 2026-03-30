#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.Quests;

/// <summary>
/// Expanded quest system — daily quests, weekly quests, story quests, and factions.
/// Works alongside the existing QuestService (which handles the original daily IQuest system).
/// </summary>
public sealed class ExpandedQuestService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    // Static reference for game modules to progress quests without DI
    private static ExpandedQuestService _instance;

    public ExpandedQuestService(DbService db, ICurrencyService cs)
    {
        _instance = this;
        _db = db;
        _cs = cs;
    }

    // ═══════════════════════════════════════════════════════════
    //  QUEST TEMPLATE DEFINITIONS
    // ═══════════════════════════════════════════════════════════

    public record QuestTemplate(
        string Id,
        string Name,
        string Description,
        string Type,
        int RequiredProgress,
        long XpReward,
        long CurrencyReward,
        string FactionAffinity = null
    );

    // ── DAILY QUEST TEMPLATES (30+) ──────────────────────────

    private static readonly QuestTemplate[] DailyTemplates =
    [
        new("daily_send_messages",        "Chatterbox",          "Send 50 messages in chat",                   "Daily", 50,   100,  200),
        new("daily_react_messages",       "Reaction King",       "React to 20 messages",                       "Daily", 20,   80,   150),
        new("daily_win_minigame",         "Game Winner",         "Win a mini-game",                            "Daily", 1,    120,  250),
        new("daily_feed_pet",             "Pet Parent",          "Feed your pet",                              "Daily", 1,    60,   100),
        new("daily_complete_dungeon",     "Dungeon Crawler",     "Complete a dungeon run",                     "Daily", 1,    150,  300),
        new("daily_give_compliments",     "Kind Soul",           "Give 5 compliments to others",               "Daily", 5,    80,   120),
        new("daily_use_commands",         "Command Master",      "Use 10 bot commands",                        "Daily", 10,   70,   100),
        new("daily_spend_currency",       "Big Spender",         "Spend 1000 currency",                        "Daily", 1000, 100,  200),
        new("daily_earn_xp",             "XP Grinder",          "Earn 500 XP",                                "Daily", 500,  120,  180),
        new("daily_voice_time",          "Voice Warrior",       "Spend 30 minutes in voice chat",             "Daily", 30,   100,  200),
        new("daily_catch_fish",          "Fisher Folk",         "Catch 5 fish",                               "Daily", 5,    90,   150),
        new("daily_place_bets",          "Risk Taker",          "Place 3 bets",                               "Daily", 3,    80,   130),
        new("daily_win_hangman",         "Word Wizard",         "Win a game of Hangman",                      "Daily", 1,    100,  200),
        new("daily_gift_waifu",          "Generous Lover",      "Gift a waifu",                               "Daily", 1,    90,   170),
        new("daily_plant_flowers",       "Gardener",            "Plant or pick 3 flowers",                    "Daily", 3,    70,   110),
        new("daily_join_race",           "Racer",               "Join an animal race",                        "Daily", 1,    80,   140),
        new("daily_bank_deposit",        "Banker",              "Make a bank deposit",                        "Daily", 1,    60,   100),
        new("daily_set_pixels",          "Pixel Artist",        "Set 10 pixels on the canvas",                "Daily", 10,   90,   150),
        new("daily_earn_karma",          "Good Vibes",          "Earn 10 karma points",                       "Daily", 10,   80,   130),
        new("daily_trade_items",         "Trader",              "Trade with another player",                  "Daily", 1,    100,  180),
        new("daily_craft_item",          "Artisan",             "Craft an item",                              "Daily", 1,    90,   160),
        new("daily_collect_daily",       "Daily Collector",     "Collect your daily reward",                  "Daily", 1,    50,   80),
        new("daily_win_trivia",          "Trivia Buff",         "Answer 3 trivia questions correctly",        "Daily", 3,    100,  170),
        new("daily_send_images",         "Photographer",        "Share 5 images in chat",                     "Daily", 5,    70,   100),
        new("daily_use_emotes",          "Emote Spammer",       "Use 20 custom emotes",                       "Daily", 20,   60,   90),
        new("daily_help_newbie",         "Mentor",              "Help a new member (welcome them)",           "Daily", 1,    110,  200),
        new("daily_level_up",            "Level Up!",           "Gain a level",                               "Daily", 1,    150,  250),
        new("daily_check_leaderboard",   "Scout",               "Check the leaderboards",                     "Daily", 1,    50,   70),
        new("daily_afk_return",          "I'm Back!",           "Set and return from AFK",                    "Daily", 1,    60,   80),
        new("daily_pet_adventure",       "Explorer",            "Send your pet on an adventure",              "Daily", 1,    100,  160),
        new("daily_pvp_battle",          "Challenger",          "Challenge someone to a PvP battle",          "Daily", 1,    120,  200),
        new("daily_collect_cards",       "Collector",           "Collect 3 cards",                            "Daily", 3,    90,   150),
    ];

    // ── WEEKLY QUEST TEMPLATES (15+) ─────────────────────────

    private static readonly QuestTemplate[] WeeklyTemplates =
    [
        new("weekly_complete_dungeons",    "Dungeon Master",       "Complete 10 dungeon runs",              "Weekly", 10,   800,  1500, "Warriors Guild"),
        new("weekly_win_pvp",             "Arena Champion",       "Win 5 PvP battles",                    "Weekly", 5,    700,  1200, "Warriors Guild"),
        new("weekly_craft_items",         "Master Crafter",       "Craft 20 items",                       "Weekly", 20,   600,  1000, "Merchants Guild"),
        new("weekly_earn_karma",          "Karma King",           "Earn 50 karma points",                 "Weekly", 50,   500,  900),
        new("weekly_raid_boss",           "Raid Veteran",         "Participate in a raid boss fight",      "Weekly", 1,    1000, 2000, "Warriors Guild"),
        new("weekly_catch_fish",          "Master Angler",        "Catch 30 fish",                         "Weekly", 30,   600,  1100),
        new("weekly_send_messages",       "Social Butterfly",     "Send 500 messages",                     "Weekly", 500,  700,  1300),
        new("weekly_earn_xp",            "XP Legend",            "Earn 5000 XP this week",                "Weekly", 5000, 900,  1600, "Scholars Circle"),
        new("weekly_win_trivia",         "Trivia Master",        "Answer 20 trivia questions correctly",  "Weekly", 20,   800,  1400, "Scholars Circle"),
        new("weekly_trade_items",        "Trade Baron",          "Complete 10 trades",                    "Weekly", 10,   700,  1200, "Merchants Guild"),
        new("weekly_voice_hours",        "Voice Legend",         "Spend 5 hours in voice chat",           "Weekly", 300,  800,  1500),
        new("weekly_collect_cards",      "Card Collector",       "Collect 15 cards",                      "Weekly", 15,   600,  1100),
        new("weekly_place_bets",         "High Roller",          "Place 20 bets",                         "Weekly", 20,   700,  1300, "Merchants Guild"),
        new("weekly_complete_dailies",   "Daily Devotee",        "Complete all daily quests 5 days",      "Weekly", 5,    1000, 1800),
        new("weekly_hidden_objectives",  "Shadow Agent",         "Complete 3 hidden objectives",          "Weekly", 3,    900,  1600, "Shadow Brotherhood"),
        new("weekly_level_ups",          "Power Leveler",        "Gain 3 levels",                         "Weekly", 3,    850,  1500, "Scholars Circle"),
    ];

    // ── STORY QUEST CHAIN (10 sequential) ────────────────────

    private static readonly QuestTemplate[] StoryTemplates =
    [
        new("story_01_awakening",       "The Awakening",         "You hear a mysterious voice calling you. Send 10 messages to respond.",
            "Story", 10,   200,  500),
        new("story_02_first_steps",     "First Steps",           "The voice guides you to prove your worth. Complete 2 dungeon runs.",
            "Story", 2,    400,  800),
        new("story_03_dark_forest",     "The Dark Forest",       "A dark forest blocks your path. Catch 10 fish to sustain yourself on the journey.",
            "Story", 10,   500,  1000),
        new("story_04_ancient_ruins",   "Ancient Ruins",         "You discover ancient ruins filled with puzzles. Answer 5 trivia questions to decipher the clues.",
            "Story", 5,    600,  1200),
        new("story_05_lost_artifact",   "The Lost Artifact",     "Deep in the ruins lies a powerful artifact. Earn 2000 XP to unlock its power.",
            "Story", 2000, 800,  1500),
        new("story_06_shadows_edge",    "Shadow's Edge",         "Dark forces sense the artifact's power. Win 3 PvP battles to defend yourself.",
            "Story", 3,    900,  1800),
        new("story_07_dragons_lair",    "Dragon's Lair",         "A dragon guards the final gate. Complete 5 dungeon runs to reach its lair.",
            "Story", 5,    1000, 2000),
        new("story_08_final_battle",    "The Final Battle",      "Face the dragon in an epic showdown. Participate in a raid boss event.",
            "Story", 1,    1500, 3000),
        new("story_09_victory",         "Victory",               "You have triumphed! Celebrate by earning 10000 XP to cement your legend.",
            "Story", 10000,2000, 5000),
        new("story_10_legend_continues","The Legend Continues",  "Your story is not over. Complete 20 quests of any type to unlock your true potential.",
            "Story", 20,   3000, 8000),
    ];

    // ── FACTION DEFINITIONS ──────────────────────────────────

    public static readonly string[] FactionNames =
    [
        "Warriors Guild",
        "Merchants Guild",
        "Scholars Circle",
        "Shadow Brotherhood"
    ];

    private static readonly Dictionary<string, string> FactionDescriptions = new()
    {
        ["Warriors Guild"]     = "Combat-focused warriors who thrive in dungeons and raids. Prove your strength in battle!",
        ["Merchants Guild"]    = "Economy masters who profit from trading, crafting, and shrewd investments. Build your fortune!",
        ["Scholars Circle"]    = "Knowledge seekers who value XP, trivia, and intellectual pursuits. Expand your mind!",
        ["Shadow Brotherhood"] = "Mysterious operatives who excel at hidden objectives and covert missions. Walk unseen!"
    };

    private static readonly Dictionary<string, string> FactionEmojis = new()
    {
        ["Warriors Guild"]     = "\u2694\uFE0F",
        ["Merchants Guild"]    = "\uD83D\uDCB0",
        ["Scholars Circle"]    = "\uD83D\uDCDA",
        ["Shadow Brotherhood"] = "\uD83D\uDC7B"
    };

    private static readonly (int Threshold, string Rank)[] FactionRanks =
    [
        (0,    "Outsider"),
        (100,  "Initiate"),
        (500,  "Member"),
        (1500, "Veteran"),
        (4000, "Champion"),
        (10000,"Legend"),
    ];

    // ═══════════════════════════════════════════════════════════
    //  QUEST LOG (lifetime stats)
    // ═══════════════════════════════════════════════════════════

    public async Task<QuestLog> GetOrCreateQuestLogAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var log = await ctx.GetTable<QuestLog>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (log is not null)
            return log;

        log = new QuestLog
        {
            UserId = userId,
            GuildId = guildId,
            TotalQuestsCompleted = 0,
            DailyQuestsCompleted = 0,
            WeeklyQuestsCompleted = 0,
            StoryQuestsCompleted = 0,
            CurrentStreak = 0,
            BestStreak = 0,
            LastDailyRefresh = DateTime.MinValue,
            LastWeeklyRefresh = DateTime.MinValue,
            DateAdded = DateTime.UtcNow
        };

        log.Id = await ctx.GetTable<QuestLog>()
            .InsertWithInt32IdentityAsync(() => new QuestLog
            {
                UserId = userId,
                GuildId = guildId,
                TotalQuestsCompleted = 0,
                DailyQuestsCompleted = 0,
                WeeklyQuestsCompleted = 0,
                StoryQuestsCompleted = 0,
                CurrentStreak = 0,
                BestStreak = 0,
                LastDailyRefresh = DateTime.MinValue,
                LastWeeklyRefresh = DateTime.MinValue,
                DateAdded = DateTime.UtcNow
            });

        return log;
    }

    // ═══════════════════════════════════════════════════════════
    //  GET ACTIVE QUESTS
    // ═══════════════════════════════════════════════════════════

    public async Task<List<QuestProgress>> GetActiveQuestsAsync(ulong userId, ulong guildId, string type = null)
    {
        await using var ctx = _db.GetDbContext();

        var query = ctx.GetTable<QuestProgress>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.Status == "Active");

        if (type is not null)
            query = query.Where(x => x.Type == type);

        return await query.OrderBy(x => x.StartedAt).ToListAsyncLinqToDB();
    }

    public async Task<List<QuestProgress>> GetCompletedQuestsAsync(ulong userId, ulong guildId, string type = null)
    {
        await using var ctx = _db.GetDbContext();

        var query = ctx.GetTable<QuestProgress>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.Status == "Completed");

        if (type is not null)
            query = query.Where(x => x.Type == type);

        return await query.OrderByDescending(x => x.CompletedAt).ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  GENERATE DAILY QUESTS (3 random, refreshes every 24h)
    // ═══════════════════════════════════════════════════════════

    public async Task<List<QuestProgress>> GenerateDailyQuestsAsync(ulong userId, ulong guildId)
    {
        var now = DateTime.UtcNow;
        var log = await GetOrCreateQuestLogAsync(userId, guildId);

        // Check if dailies need refreshing
        if (log.LastDailyRefresh.Date == now.Date)
        {
            // Already refreshed today — return existing active dailies
            return await GetActiveQuestsAsync(userId, guildId, "Daily");
        }

        await using var ctx = _db.GetDbContext();

        // Expire old active dailies
        await ctx.GetTable<QuestProgress>()
            .Where(x => x.UserId == userId && x.GuildId == guildId
                        && x.Type == "Daily" && x.Status == "Active")
            .Set(x => x.Status, "Failed")
            .UpdateAsync();

        // Update streak
        var yesterday = now.Date.AddDays(-1);
        var completedYesterday = await ctx.GetTable<QuestProgress>()
            .AnyAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId
                                   && x.Type == "Daily" && x.Status == "Completed"
                                   && x.CompletedAt != null && x.CompletedAt.Value.Date == yesterday);

        var newStreak = completedYesterday ? log.CurrentStreak + 1 : 0;
        var bestStreak = Math.Max(log.BestStreak, newStreak);

        // Pick 3 random daily templates
        var shuffled = DailyTemplates.ToList();
        ShuffleList(shuffled);
        var picked = shuffled.Take(3).ToList();

        var newQuests = new List<QuestProgress>();
        foreach (var t in picked)
        {
            var quest = new QuestProgress
            {
                UserId = userId,
                GuildId = guildId,
                QuestId = t.Id,
                QuestName = t.Name,
                Description = t.Description,
                Type = "Daily",
                Status = "Active",
                CurrentProgress = 0,
                RequiredProgress = t.RequiredProgress,
                XpReward = t.XpReward,
                CurrencyReward = t.CurrencyReward,
                StartedAt = now,
                ExpiresAt = now.Date.AddDays(1),
                DateAdded = now
            };

            quest.Id = await ctx.GetTable<QuestProgress>()
                .InsertWithInt32IdentityAsync(() => new QuestProgress
                {
                    UserId = userId,
                    GuildId = guildId,
                    QuestId = t.Id,
                    QuestName = t.Name,
                    Description = t.Description,
                    Type = "Daily",
                    Status = "Active",
                    CurrentProgress = 0,
                    RequiredProgress = t.RequiredProgress,
                    XpReward = t.XpReward,
                    CurrencyReward = t.CurrencyReward,
                    StartedAt = now,
                    ExpiresAt = now.Date.AddDays(1),
                    DateAdded = now
                });

            newQuests.Add(quest);
        }

        // Update quest log
        await ctx.GetTable<QuestLog>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Set(x => x.LastDailyRefresh, now)
            .Set(x => x.CurrentStreak, newStreak)
            .Set(x => x.BestStreak, bestStreak)
            .UpdateAsync();

        return newQuests;
    }

    // ═══════════════════════════════════════════════════════════
    //  GENERATE WEEKLY QUEST (1 random, refreshes every 7 days)
    // ═══════════════════════════════════════════════════════════

    public async Task<QuestProgress> GenerateWeeklyQuestAsync(ulong userId, ulong guildId)
    {
        var now = DateTime.UtcNow;
        var log = await GetOrCreateQuestLogAsync(userId, guildId);

        // Check if weekly needs refreshing (7 days since last refresh)
        if ((now - log.LastWeeklyRefresh).TotalDays < 7)
        {
            var existing = await GetActiveQuestsAsync(userId, guildId, "Weekly");
            if (existing.Count > 0)
                return existing[0];
        }

        await using var ctx = _db.GetDbContext();

        // Expire old active weeklies
        await ctx.GetTable<QuestProgress>()
            .Where(x => x.UserId == userId && x.GuildId == guildId
                        && x.Type == "Weekly" && x.Status == "Active")
            .Set(x => x.Status, "Failed")
            .UpdateAsync();

        // Pick 1 random weekly template
        var template = WeeklyTemplates[_rng.Next(WeeklyTemplates.Length)];

        var quest = new QuestProgress
        {
            UserId = userId,
            GuildId = guildId,
            QuestId = template.Id,
            QuestName = template.Name,
            Description = template.Description,
            Type = "Weekly",
            Status = "Active",
            CurrentProgress = 0,
            RequiredProgress = template.RequiredProgress,
            XpReward = template.XpReward,
            CurrencyReward = template.CurrencyReward,
            StartedAt = now,
            ExpiresAt = now.AddDays(7),
            DateAdded = now
        };

        quest.Id = await ctx.GetTable<QuestProgress>()
            .InsertWithInt32IdentityAsync(() => new QuestProgress
            {
                UserId = userId,
                GuildId = guildId,
                QuestId = template.Id,
                QuestName = template.Name,
                Description = template.Description,
                Type = "Weekly",
                Status = "Active",
                CurrentProgress = 0,
                RequiredProgress = template.RequiredProgress,
                XpReward = template.XpReward,
                CurrencyReward = template.CurrencyReward,
                StartedAt = now,
                ExpiresAt = now.AddDays(7),
                DateAdded = now
            });

        // Update quest log
        await ctx.GetTable<QuestLog>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Set(x => x.LastWeeklyRefresh, now)
            .UpdateAsync();

        return quest;
    }

    // ═══════════════════════════════════════════════════════════
    //  STORY QUESTS (sequential chain)
    // ═══════════════════════════════════════════════════════════

    public async Task<QuestProgress> GetOrStartStoryQuestAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Check for active story quest
        var active = await ctx.GetTable<QuestProgress>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId
                                              && x.Type == "Story" && x.Status == "Active");
        if (active is not null)
            return active;

        // Find next story quest in chain
        var completedIds = await ctx.GetTable<QuestProgress>()
            .Where(x => x.UserId == userId && x.GuildId == guildId
                        && x.Type == "Story" && x.Status == "Completed")
            .Select(x => x.QuestId)
            .ToListAsyncLinqToDB();

        QuestTemplate nextTemplate = null;
        foreach (var st in StoryTemplates)
        {
            if (!completedIds.Contains(st.Id))
            {
                nextTemplate = st;
                break;
            }
        }

        if (nextTemplate is null)
            return null; // All story quests completed!

        var now = DateTime.UtcNow;
        var quest = new QuestProgress
        {
            UserId = userId,
            GuildId = guildId,
            QuestId = nextTemplate.Id,
            QuestName = nextTemplate.Name,
            Description = nextTemplate.Description,
            Type = "Story",
            Status = "Active",
            CurrentProgress = 0,
            RequiredProgress = nextTemplate.RequiredProgress,
            XpReward = nextTemplate.XpReward,
            CurrencyReward = nextTemplate.CurrencyReward,
            StartedAt = now,
            ExpiresAt = null, // Story quests never expire
            DateAdded = now
        };

        quest.Id = await ctx.GetTable<QuestProgress>()
            .InsertWithInt32IdentityAsync(() => new QuestProgress
            {
                UserId = userId,
                GuildId = guildId,
                QuestId = nextTemplate.Id,
                QuestName = nextTemplate.Name,
                Description = nextTemplate.Description,
                Type = "Story",
                Status = "Active",
                CurrentProgress = 0,
                RequiredProgress = nextTemplate.RequiredProgress,
                XpReward = nextTemplate.XpReward,
                CurrencyReward = nextTemplate.CurrencyReward,
                StartedAt = now,
                ExpiresAt = null,
                DateAdded = now
            });

        return quest;
    }

    public int GetStoryProgress(List<QuestProgress> completedStory)
        => completedStory.Count;

    public int TotalStoryQuests
        => StoryTemplates.Length;

    /// <summary>Progress a quest from any module without DI (fire and forget)</summary>
    public static void Progress(ulong userId, ulong guildId, string questId, int amount = 1)
    {
        if (_instance is null) return;
        _ = Task.Run(async () =>
        {
            try { await _instance.ProgressQuestAsync(userId, guildId, questId, amount); }
            catch { /* non-critical */ }
        });
    }

    // ═══════════════════════════════════════════════════════════
    //  PROGRESS & COMPLETION
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> ProgressQuestAsync(ulong userId, ulong guildId, string questId, int amount = 1)
    {
        await using var ctx = _db.GetDbContext();

        var quest = await ctx.GetTable<QuestProgress>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId
                                              && x.QuestId == questId && x.Status == "Active");

        if (quest is null)
            return false;

        var newProgress = Math.Min(quest.CurrentProgress + amount, quest.RequiredProgress);

        await ctx.GetTable<QuestProgress>()
            .Where(x => x.Id == quest.Id)
            .Set(x => x.CurrentProgress, newProgress)
            .UpdateAsync();

        return true;
    }

    public async Task<(bool Success, QuestProgress Quest, long CurrencyAwarded)> CompleteQuestAsync(
        ulong userId, ulong guildId, string questId)
    {
        await using var ctx = _db.GetDbContext();

        var quest = await ctx.GetTable<QuestProgress>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId
                                              && x.QuestId == questId && x.Status == "Active");

        if (quest is null)
            return (false, null, 0);

        if (quest.CurrentProgress < quest.RequiredProgress)
            return (false, quest, 0);

        var now = DateTime.UtcNow;

        // Mark as completed
        await ctx.GetTable<QuestProgress>()
            .Where(x => x.Id == quest.Id)
            .Set(x => x.Status, "Completed")
            .Set(x => x.CompletedAt, now)
            .UpdateAsync();

        quest.Status = "Completed";
        quest.CompletedAt = now;

        // Award currency
        if (quest.CurrencyReward > 0)
        {
            await _cs.AddAsync(userId, quest.CurrencyReward,
                new TxData("quest", $"{quest.Type.ToLowerInvariant()}:{quest.QuestName}"));
        }

        // Update quest log stats
        var log = await GetOrCreateQuestLogAsync(userId, guildId);
        await ctx.GetTable<QuestLog>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .Set(x => x.TotalQuestsCompleted, log.TotalQuestsCompleted + 1)
            .Set(x => x.DailyQuestsCompleted, quest.Type == "Daily" ? log.DailyQuestsCompleted + 1 : log.DailyQuestsCompleted)
            .Set(x => x.WeeklyQuestsCompleted, quest.Type == "Weekly" ? log.WeeklyQuestsCompleted + 1 : log.WeeklyQuestsCompleted)
            .Set(x => x.StoryQuestsCompleted, quest.Type == "Story" ? log.StoryQuestsCompleted + 1 : log.StoryQuestsCompleted)
            .UpdateAsync();

        // Award faction rep if applicable
        var template = FindTemplate(quest.QuestId);
        if (template?.FactionAffinity is not null)
        {
            await AddFactionRepAsync(userId, guildId, template.FactionAffinity, quest.Type switch
            {
                "Daily" => 10,
                "Weekly" => 50,
                "Story" => 30,
                _ => 5
            });
        }

        return (true, quest, quest.CurrencyReward);
    }

    private QuestTemplate FindTemplate(string questId)
    {
        return DailyTemplates.FirstOrDefault(t => t.Id == questId)
               ?? WeeklyTemplates.FirstOrDefault(t => t.Id == questId)
               ?? StoryTemplates.FirstOrDefault(t => t.Id == questId);
    }

    // ═══════════════════════════════════════════════════════════
    //  FACTIONS
    // ═══════════════════════════════════════════════════════════

    public async Task<FactionStanding> GetFactionStandingAsync(ulong userId, ulong guildId, string factionName)
    {
        await using var ctx = _db.GetDbContext();

        var standing = await ctx.GetTable<FactionStanding>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId
                                              && x.FactionName == factionName);

        if (standing is not null)
            return standing;

        standing = new FactionStanding
        {
            UserId = userId,
            GuildId = guildId,
            FactionName = factionName,
            Reputation = 0,
            Rank = "Outsider",
            DateAdded = DateTime.UtcNow
        };

        standing.Id = await ctx.GetTable<FactionStanding>()
            .InsertWithInt32IdentityAsync(() => new FactionStanding
            {
                UserId = userId,
                GuildId = guildId,
                FactionName = factionName,
                Reputation = 0,
                Rank = "Outsider",
                DateAdded = DateTime.UtcNow
            });

        return standing;
    }

    public async Task<List<FactionStanding>> GetAllFactionStandingsAsync(ulong userId, ulong guildId)
    {
        var standings = new List<FactionStanding>();
        foreach (var faction in FactionNames)
        {
            standings.Add(await GetFactionStandingAsync(userId, guildId, faction));
        }
        return standings;
    }

    public async Task<FactionStanding> AddFactionRepAsync(ulong userId, ulong guildId, string factionName, int amount)
    {
        var standing = await GetFactionStandingAsync(userId, guildId, factionName);
        var newRep = Math.Max(0, standing.Reputation + amount);
        var newRank = CalculateRank(newRep);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<FactionStanding>()
            .Where(x => x.Id == standing.Id)
            .Set(x => x.Reputation, newRep)
            .Set(x => x.Rank, newRank)
            .UpdateAsync();

        standing.Reputation = newRep;
        standing.Rank = newRank;
        return standing;
    }

    private static string CalculateRank(int reputation)
    {
        var rank = "Outsider";
        foreach (var (threshold, name) in FactionRanks)
        {
            if (reputation >= threshold)
                rank = name;
            else
                break;
        }
        return rank;
    }

    // ═══════════════════════════════════════════════════════════
    //  FACTION INFO (public accessors)
    // ═══════════════════════════════════════════════════════════

    public static string GetFactionDescription(string factionName)
        => FactionDescriptions.GetValueOrDefault(factionName, "Unknown faction.");

    public static string GetFactionEmoji(string factionName)
        => FactionEmojis.GetValueOrDefault(factionName, "\u2753");

    public static string GetNextRank(string currentRank)
    {
        for (var i = 0; i < FactionRanks.Length - 1; i++)
        {
            if (FactionRanks[i].Rank == currentRank)
                return FactionRanks[i + 1].Rank;
        }
        return null; // Already max rank
    }

    public static int GetNextRankThreshold(string currentRank)
    {
        for (var i = 0; i < FactionRanks.Length - 1; i++)
        {
            if (FactionRanks[i].Rank == currentRank)
                return FactionRanks[i + 1].Threshold;
        }
        return -1; // Already max rank
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private static void ShuffleList<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static string ProgressBar(int current, int required, int barLength = 10)
    {
        var filled = required > 0 ? (int)((double)current / required * barLength) : 0;
        filled = Math.Min(filled, barLength);
        var empty = barLength - filled;
        return new string('\u2588', filled) + new string('\u2591', empty)
               + $" [{current}/{required}]";
    }
}
