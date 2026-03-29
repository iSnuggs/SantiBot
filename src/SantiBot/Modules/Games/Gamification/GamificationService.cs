#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;
using System.Text;

namespace SantiBot.Modules.Games.Gamification;

public sealed class GamificationService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    public GamificationService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    // ═══════════════════════════════════════════════════════════
    //  BADGES — 100 collectible badges
    // ═══════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Emoji, string Category, string Rarity, string HowToEarn)[] AllBadges =
    [
        // Combat (20)
        ("combat_first_blood", "First Blood", "🩸", "Combat", "Common", "Win your first duel"),
        ("combat_warrior", "Warrior Spirit", "⚔️", "Combat", "Common", "Win 10 duels"),
        ("combat_gladiator", "Gladiator", "🏛️", "Combat", "Uncommon", "Win 25 duels"),
        ("combat_champion", "Champion", "🏆", "Combat", "Rare", "Win 50 duels"),
        ("combat_legend", "PvP Legend", "👑", "Combat", "Epic", "Win 100 duels"),
        ("combat_undefeated", "Undefeated", "💎", "Combat", "Legendary", "Win 10 duels in a row"),
        ("combat_raid_slayer", "Raid Slayer", "🐉", "Combat", "Uncommon", "Defeat a raid boss"),
        ("combat_raid_hero", "Raid Hero", "⚡", "Combat", "Rare", "Be MVP in a raid"),
        ("combat_dungeon_master", "Dungeon Master", "🏰", "Combat", "Rare", "Clear 100 dungeons"),
        ("combat_boss_rush", "Boss Rush", "👹", "Combat", "Epic", "Clear 5 dungeons in one day"),
        ("combat_diff5", "Nightmare Conqueror", "💀", "Combat", "Epic", "Clear difficulty 5"),
        ("combat_all_classes", "Multiclass", "🎭", "Combat", "Rare", "Play every class"),
        ("combat_all_races", "Worldly", "🌍", "Combat", "Rare", "Play every race"),
        ("combat_phoenix", "Phoenix", "🔥", "Combat", "Uncommon", "Be saved by Phoenix Feather"),
        ("combat_clutch", "Clutch King", "😰", "Combat", "Rare", "Survive at 1 HP"),
        ("combat_party_leader", "Party Leader", "👥", "Combat", "Common", "Lead 20 dungeon parties"),
        ("combat_healer", "Lifesaver", "💚", "Combat", "Uncommon", "Heal 1000 HP as Cleric"),
        ("combat_assassin", "Assassin", "🗡️", "Combat", "Uncommon", "Kill 50 monsters with crits"),
        ("combat_tank", "Immovable", "🛡️", "Combat", "Uncommon", "Take 5000 damage total"),
        ("combat_speed", "Speed Demon", "⚡", "Combat", "Rare", "Clear a dungeon in under 3 min"),

        // Social (20)
        ("social_friendly", "Friendly", "😊", "Social", "Common", "Send 100 RP actions"),
        ("social_hugger", "Super Hugger", "🤗", "Social", "Common", "Give 50 hugs"),
        ("social_married", "Taken", "💍", "Social", "Uncommon", "Get married"),
        ("social_popular", "Popular", "🌟", "Social", "Uncommon", "Have 25 friends"),
        ("social_karma_king", "Karma King", "⭐", "Social", "Rare", "Reach 500 karma"),
        ("social_helper", "Helper", "🆘", "Social", "Common", "Help 10 users"),
        ("social_mentor", "Mentor", "🎓", "Social", "Rare", "Help 50 users"),
        ("social_greeter", "Welcomer", "👋", "Social", "Common", "Welcome 20 new members"),
        ("social_confessor", "Confessor", "🤫", "Social", "Uncommon", "Submit 10 confessions"),
        ("social_event_goer", "Event Goer", "🎪", "Social", "Common", "Attend 10 events"),
        ("social_event_host", "Event Host", "🎤", "Social", "Uncommon", "Host 5 events"),
        ("social_recruiter", "Recruiter", "📨", "Social", "Uncommon", "Invite 10 members"),
        ("social_booster", "Booster", "💎", "Social", "Rare", "Boost the server"),
        ("social_og", "OG Member", "🏛️", "Social", "Epic", "Be one of the first 50 members"),
        ("social_yearbook", "Yearbook Star", "📸", "Social", "Uncommon", "Get featured in yearbook"),
        ("social_matchmaker", "Matchmaker", "💘", "Social", "Uncommon", "Check compatibility 50 times"),
        ("social_birthday", "Birthday Star", "🎂", "Social", "Common", "Be active on your birthday"),
        ("social_night_owl", "Night Owl", "🦉", "Social", "Common", "Chat after midnight 20 times"),
        ("social_early_bird", "Early Bird", "🐦", "Social", "Common", "Chat before 6 AM 10 times"),
        ("social_center", "Center of Attention", "🌐", "Social", "Epic", "Have 50 friends"),

        // Economy (20)
        ("econ_first_coin", "First Coin", "🪙", "Economy", "Common", "Earn your first currency"),
        ("econ_thousand", "Stacked", "💰", "Economy", "Common", "Have 1,000 currency"),
        ("econ_ten_k", "Wealthy", "💎", "Economy", "Uncommon", "Have 10,000 currency"),
        ("econ_hundred_k", "Rich", "🤑", "Economy", "Rare", "Have 100,000 currency"),
        ("econ_million", "Millionaire", "👑", "Economy", "Epic", "Have 1,000,000 currency"),
        ("econ_gambler", "Gambler", "🎰", "Economy", "Common", "Win 25 gambles"),
        ("econ_jackpot", "Jackpot!", "💵", "Economy", "Rare", "Hit a jackpot"),
        ("econ_trader", "Trader", "🤝", "Economy", "Uncommon", "Complete 50 trades"),
        ("econ_shopkeeper", "Shopkeeper", "🏪", "Economy", "Uncommon", "Open your own shop"),
        ("econ_investor", "Investor", "📈", "Economy", "Uncommon", "Make profit on stocks"),
        ("econ_miner", "Miner", "⛏️", "Economy", "Common", "Mine 100 ores"),
        ("econ_farmer", "Farmer", "🌾", "Economy", "Common", "Harvest 100 crops"),
        ("econ_chef", "Chef", "👨‍🍳", "Economy", "Uncommon", "Cook 50 meals"),
        ("econ_alchemist", "Alchemist", "🧪", "Economy", "Uncommon", "Brew 50 potions"),
        ("econ_blacksmith", "Blacksmith", "🔥", "Economy", "Uncommon", "Forge 50 items"),
        ("econ_fisher", "Fisher", "🐟", "Economy", "Common", "Catch 100 fish"),
        ("econ_crafter", "Master Crafter", "⚒️", "Economy", "Rare", "Craft 200 items"),
        ("econ_generous", "Philanthropist", "🎁", "Economy", "Rare", "Give away 50,000 currency"),
        ("econ_daily_streak", "Daily Devotee", "📅", "Economy", "Uncommon", "100-day daily streak"),
        ("econ_broke", "Broke", "😭", "Economy", "Common", "Lose all currency gambling"),

        // Games (20)
        ("games_trivia_buff", "Trivia Buff", "🧠", "Games", "Common", "Answer 50 trivia correctly"),
        ("games_trivia_god", "Trivia God", "👑", "Games", "Epic", "Answer 500 trivia correctly"),
        ("games_chess_master", "Chess Master", "♟️", "Games", "Rare", "Win 25 chess games"),
        ("games_br_winner", "Battle Royale Victor", "🏆", "Games", "Uncommon", "Win a Battle Royale"),
        ("games_mafia_boss", "Mafia Boss", "🕶️", "Games", "Uncommon", "Win 10 Mafia games"),
        ("games_pet_master", "Pet Master", "🐾", "Games", "Rare", "Have 10 evolved pets"),
        ("games_pet_shiny", "Shiny Hunter", "✨", "Games", "Epic", "Find a shiny pet"),
        ("games_fish_legend", "Legendary Fisher", "🐋", "Games", "Epic", "Catch a legendary fish"),
        ("games_card_collector", "Card Collector", "🃏", "Games", "Uncommon", "Collect 50 cards"),
        ("games_puzzle_solver", "Puzzle Solver", "🧩", "Games", "Common", "Solve 25 puzzles"),
        ("games_story_complete", "Story Complete", "📖", "Games", "Rare", "Finish a story quest"),
        ("games_tower_master", "Tower Master", "🗼", "Games", "Rare", "Reach wave 50 in Tower Defense"),
        ("games_speed_typer", "Speed Typer", "⌨️", "Games", "Uncommon", "Type 100+ WPM"),
        ("games_racing_champ", "Racing Champion", "🏎️", "Games", "Uncommon", "Win 10 races"),
        ("games_idle_master", "Idle Master", "🖱️", "Games", "Common", "Reach idle level 50"),
        ("games_quest_hero", "Quest Hero", "📜", "Games", "Uncommon", "Complete 50 quests"),
        ("games_quest_legend", "Quest Legend", "🌟", "Games", "Epic", "Complete 200 quests"),
        ("games_variety", "Game Hopper", "🎮", "Games", "Uncommon", "Play 15 different games"),
        ("games_win_streak", "Win Streak", "🔥", "Games", "Rare", "Win 10 games in a row"),
        ("games_tournament", "Tournament Winner", "🥇", "Games", "Epic", "Win a tournament"),

        // Special (20)
        ("special_founder", "Founder", "🏛️", "Special", "Legendary", "Be in the server from day one"),
        ("special_1_year", "One Year", "🎂", "Special", "Rare", "Be a member for 1 year"),
        ("special_2_years", "Two Years", "🎊", "Special", "Epic", "Be a member for 2 years"),
        ("special_completionist", "Completionist", "🏆", "Special", "Legendary", "Earn 90% of all badges"),
        ("special_all_systems", "Well Rounded", "🔄", "Special", "Epic", "Use every bot system"),
        ("special_holiday_collector", "Holiday Collector", "🎄", "Special", "Rare", "Be active on 5 holidays"),
        ("special_bug_finder", "Bug Hunter", "🐛", "Special", "Rare", "Report a bug that gets fixed"),
        ("special_beta_tester", "Beta Tester", "🧪", "Special", "Epic", "Test a feature before launch"),
        ("special_streamer", "Streamer", "📺", "Special", "Uncommon", "Stream for 10 hours"),
        ("special_lorekeeper", "Lorekeeper", "📚", "Special", "Rare", "Read all lore entries"),
        ("special_fashion", "Fashionista", "👗", "Special", "Uncommon", "Customize your profile fully"),
        ("special_collector", "Collector", "🎒", "Special", "Rare", "Own 100 unique items"),
        ("special_explorer", "Explorer", "🗺️", "Special", "Uncommon", "Use every command category"),
        ("special_night_watch", "Night Watch", "🌙", "Special", "Uncommon", "Be last active 10 times"),
        ("special_speed_demon", "Speed Demon", "⚡", "Special", "Uncommon", "React within 1 second"),
        ("special_palindrome", "Palindrome", "🪞", "Special", "Common", "Send a palindrome message"),
        ("special_lucky_777", "Lucky 777", "🍀", "Special", "Rare", "Be the 777th daily claimer"),
        ("special_over_9000", "Over 9000!", "💥", "Special", "Epic", "Have any stat exceed 9000"),
        ("special_secret_finder", "Secret Finder", "🗝️", "Special", "Legendary", "Find all hidden secrets"),
        ("special_mythic", "Mythic", "🔱", "Special", "Legendary", "Defeat the Abyssal Behemoth"),
    ];

    // ═══════════════════════════════════════════════════════════
    //  TITLES — 50 earnable display titles
    // ═══════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Color, string Requirement)[] AllTitles =
    [
        // Combat titles
        ("title_adventurer", "Adventurer", "#4CAF50", "Complete first dungeon"),
        ("title_warrior", "Warrior", "#F44336", "Win 25 duels"),
        ("title_champion", "Champion", "#FF9800", "Win a tournament"),
        ("title_legend", "Legend", "#9C27B0", "Win 100 duels"),
        ("title_godslayer", "Godslayer", "#E91E63", "Defeat the Abyssal Behemoth"),
        ("title_dragon_slayer", "Dragon Slayer", "#FF5722", "Defeat the Infernal Dragon"),
        ("title_dungeon_lord", "Dungeon Lord", "#795548", "Clear 500 dungeons"),
        ("title_raid_commander", "Raid Commander", "#607D8B", "Be MVP in 10 raids"),
        ("title_death_cheater", "Death Cheater", "#9E9E9E", "Survive at 1 HP 5 times"),
        ("title_berserker", "Berserker", "#D32F2F", "Deal 100,000 total dungeon damage"),

        // Social titles
        ("title_beloved", "Beloved", "#E91E63", "Reach 1000 karma"),
        ("title_social_butterfly", "Social Butterfly", "#FF4081", "Have 50 friends"),
        ("title_matchmaker", "Matchmaker", "#F48FB1", "Marry someone"),
        ("title_mentor", "Mentor", "#3F51B5", "Help 100 users"),
        ("title_ambassador", "Ambassador", "#00BCD4", "Invite 50 members"),
        ("title_comedian", "Comedian", "#FFC107", "Get 100 reactions on a message"),
        ("title_elder", "Elder", "#8D6E63", "Be a member for 2 years"),

        // Economy titles
        ("title_merchant", "Merchant", "#4CAF50", "Complete 100 trades"),
        ("title_tycoon", "Tycoon", "#FF9800", "Have 1,000,000 currency"),
        ("title_master_crafter", "Master Crafter", "#795548", "Craft 500 items"),
        ("title_alchemist", "Grand Alchemist", "#9C27B0", "Brew 100 potions"),
        ("title_blacksmith", "Master Blacksmith", "#F44336", "Forge 100 items"),
        ("title_farmer", "Arch Farmer", "#4CAF50", "Harvest 500 crops"),
        ("title_miner", "Deep Miner", "#607D8B", "Mine 500 ores"),
        ("title_fisher", "Grand Fisher", "#2196F3", "Catch 500 fish"),

        // XP/Level titles
        ("title_centurion", "Centurion", "#FFD700", "Reach level 100"),
        ("title_mythical", "Mythical", "#E040FB", "Reach level 200"),
        ("title_prestige", "Prestige", "#B388FF", "Prestige once"),
        ("title_prestige_x", "Prestige X", "#7C4DFF", "Prestige 10 times"),
        ("title_xp_champion", "XP Champion", "#00E676", "Be #1 XP for a month"),

        // Game titles
        ("title_trivia_master", "Trivia Master", "#2196F3", "Answer 1000 trivia correctly"),
        ("title_chess_grandmaster", "Grandmaster", "#9E9E9E", "Win 50 chess games"),
        ("title_beast_master", "Beast Master", "#8BC34A", "Own 20 pets"),
        ("title_card_king", "Card King", "#FF6F00", "Complete a card collection"),
        ("title_quest_hero", "Quest Hero", "#00BCD4", "Complete 100 quests"),
        ("title_story_teller", "Storyteller", "#7B1FA2", "Complete all story quests"),
        ("title_game_master", "Game Master", "#F44336", "Win 500 total games"),

        // Special titles
        ("title_founder", "Founder", "#FFD700", "Be a founding member"),
        ("title_completionist", "Completionist", "#E040FB", "Earn 90% of badges"),
        ("title_collector", "Collector", "#FF6F00", "Own 500 unique items"),
        ("title_explorer", "Explorer", "#00BCD4", "Use every bot feature"),
        ("title_streamer", "Content Creator", "#9C27B0", "Stream 50 hours"),
        ("title_mod", "Moderator", "#4CAF50", "Perform 100 mod actions"),
        ("title_guardian", "Guardian", "#2196F3", "Block 5 raids"),
        ("title_philanthropist", "Philanthropist", "#E91E63", "Give away 100,000 currency"),
        ("title_night_king", "Night King", "#311B92", "Be active at midnight 100 times"),
        ("title_speedrunner", "Speedrunner", "#00E5FF", "Set 3 speed records"),
        ("title_immortal", "Immortal", "#FFD700", "365-day activity streak"),
        ("title_ascended", "Ascended", "#E040FB", "Earn every other title"),
        ("title_custom", "Custom", "#FFFFFF", "Win a special event (admin granted)"),
    ];

    // ═══════════════════════════════════════════════════════════
    //  BATTLE PASS REWARDS (50 tiers)
    // ═══════════════════════════════════════════════════════════

    public static readonly (int Tier, string FreeReward, string PremiumReward)[] BattlePassTiers =
    [
        (1,  "100 Currency", "200 Currency + XP Booster (1hr)"),
        (2,  "Badge: Season Starter", "Badge: Premium Starter"),
        (3,  "200 Currency", "500 Currency"),
        (4,  "50 XP Bonus", "150 XP Bonus"),
        (5,  "Lootbox x1", "Lootbox x3"),
        (6,  "300 Currency", "600 Currency + Pet Food x5"),
        (7,  "Badge: Tier 7", "Title: Season Warrior"),
        (8,  "100 XP Bonus", "300 XP Bonus"),
        (9,  "400 Currency", "800 Currency"),
        (10, "Lootbox x2 + Badge", "Lootbox x5 + Rare Badge"),
        (11, "200 Currency", "500 Currency + XP Booster (2hr)"),
        (12, "150 XP Bonus", "400 XP Bonus"),
        (13, "500 Currency", "1000 Currency"),
        (14, "Badge: Halfway There", "Badge: Premium Progress"),
        (15, "Lootbox x2", "Lootbox x5 + Rare Item"),
        (16, "300 Currency", "700 Currency"),
        (17, "200 XP Bonus", "500 XP Bonus"),
        (18, "600 Currency", "1200 Currency"),
        (19, "Pet Food x10", "Pet Food x20 + Shiny Potion"),
        (20, "Lootbox x3 + Title", "Lootbox x7 + Epic Badge"),
        (21, "400 Currency", "800 Currency + XP Booster (3hr)"),
        (22, "250 XP Bonus", "600 XP Bonus"),
        (23, "700 Currency", "1500 Currency"),
        (24, "Badge: Dedicated", "Badge: Premium Dedicated"),
        (25, "Lootbox x3 + Rare Badge", "Lootbox x8 + Epic Title"),
        (26, "500 Currency", "1000 Currency"),
        (27, "300 XP Bonus", "700 XP Bonus"),
        (28, "800 Currency", "1600 Currency"),
        (29, "Profile Background", "Premium Profile Background"),
        (30, "Lootbox x4 + Epic Badge", "Lootbox x10 + Legendary Item"),
        (31, "600 Currency", "1200 Currency + XP Booster (4hr)"),
        (32, "400 XP Bonus", "900 XP Bonus"),
        (33, "900 Currency", "2000 Currency"),
        (34, "Badge: Veteran", "Badge: Premium Veteran"),
        (35, "Lootbox x4", "Lootbox x10 + Rare Pet"),
        (36, "700 Currency", "1500 Currency"),
        (37, "500 XP Bonus", "1000 XP Bonus"),
        (38, "1000 Currency", "2500 Currency"),
        (39, "Title: Season Veteran", "Title: Season Elite"),
        (40, "Lootbox x5 + Epic Title", "Lootbox x12 + Legendary Badge"),
        (41, "800 Currency", "2000 Currency + XP Booster (6hr)"),
        (42, "600 XP Bonus", "1200 XP Bonus"),
        (43, "1200 Currency", "3000 Currency"),
        (44, "Badge: Almost There", "Badge: Premium Finisher"),
        (45, "Lootbox x5 + Legendary Badge", "Lootbox x15 + Mythic Item"),
        (46, "1000 Currency", "2500 Currency"),
        (47, "800 XP Bonus", "1500 XP Bonus"),
        (48, "1500 Currency", "4000 Currency"),
        (49, "Title: Season Legend", "Title: Season Mythic"),
        (50, "LEGENDARY BADGE + 5000 Currency + Unique Title", "MYTHIC BADGE + 10000 Currency + Exclusive Title + Profile Frame"),
    ];

    // ═══════════════════════════════════════════════════════════
    //  DAILY CHALLENGE TEMPLATES
    // ═══════════════════════════════════════════════════════════

    public static readonly (string Id, string Desc, long XpReward)[] DailyChallengeTemplates =
    [
        ("dc_msg_50", "Send 50 messages", 200),
        ("dc_msg_100", "Send 100 messages", 400),
        ("dc_react_20", "Add 20 reactions", 150),
        ("dc_voice_30m", "Spend 30 minutes in voice", 250),
        ("dc_voice_1h", "Spend 1 hour in voice", 500),
        ("dc_dungeon_1", "Complete a dungeon", 300),
        ("dc_dungeon_3", "Complete 3 dungeons", 600),
        ("dc_game_3", "Play 3 mini-games", 200),
        ("dc_game_win", "Win a mini-game", 250),
        ("dc_daily_claim", "Claim your daily reward", 100),
        ("dc_compliment_5", "Give 5 compliments", 150),
        ("dc_rp_10", "Use 10 RP actions", 150),
        ("dc_cmd_20", "Use 20 commands", 200),
        ("dc_pet_feed", "Feed your pet", 100),
        ("dc_pet_play", "Play with your pet", 100),
        ("dc_earn_500", "Earn 500 currency", 200),
        ("dc_spend_500", "Spend 500 currency", 200),
        ("dc_trivia_5", "Answer 5 trivia correctly", 300),
        ("dc_craft_3", "Craft 3 items", 250),
        ("dc_gather_10", "Gather 10 resources", 200),
    ];

    // ═══════════════════════════════════════════════════════════
    //  BADGE METHODS
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> AwardBadgeAsync(ulong userId, ulong guildId, string badgeId)
    {
        var def = AllBadges.FirstOrDefault(b => b.Id == badgeId);
        if (def.Id is null) return false;

        await using var ctx = _db.GetDbContext();
        var exists = await ctx.GetTable<UserBadge>()
            .AnyAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.BadgeId == badgeId);
        if (exists) return false;

        ctx.Add(new UserBadge
        {
            UserId = userId, GuildId = guildId,
            BadgeId = badgeId, BadgeName = def.Name,
            Emoji = def.Emoji, Category = def.Category,
            Rarity = def.Rarity,
        });
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserBadge>> GetBadgesAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserBadge>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .OrderByDescending(x => x.EarnedAt)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> ToggleBadgeDisplayAsync(ulong userId, ulong guildId, string badgeId)
    {
        await using var ctx = _db.GetDbContext();
        var badge = await ctx.GetTable<UserBadge>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.BadgeId == badgeId);
        if (badge is null) return false;

        await ctx.GetTable<UserBadge>()
            .Where(x => x.Id == badge.Id)
            .UpdateAsync(_ => new UserBadge { IsDisplayed = !badge.IsDisplayed });
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    //  TITLE METHODS
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> AwardTitleAsync(ulong userId, ulong guildId, string titleId)
    {
        var def = AllTitles.FirstOrDefault(t => t.Id == titleId);
        if (def.Id is null) return false;

        await using var ctx = _db.GetDbContext();
        var exists = await ctx.GetTable<UserTitle>()
            .AnyAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.TitleId == titleId);
        if (exists) return false;

        ctx.Add(new UserTitle
        {
            UserId = userId, GuildId = guildId,
            TitleId = titleId, TitleName = def.Name,
            Color = def.Color,
        });
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserTitle>> GetTitlesAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserTitle>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> EquipTitleAsync(ulong userId, ulong guildId, string titleId)
    {
        await using var ctx = _db.GetDbContext();
        var title = await ctx.GetTable<UserTitle>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.TitleId == titleId);
        if (title is null) return false;

        // Unequip all other titles
        await ctx.GetTable<UserTitle>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.IsActive)
            .UpdateAsync(_ => new UserTitle { IsActive = false });

        // Equip this one
        await ctx.GetTable<UserTitle>()
            .Where(x => x.Id == title.Id)
            .UpdateAsync(_ => new UserTitle { IsActive = true });
        return true;
    }

    public async Task<UserTitle> GetActiveTitleAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserTitle>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.IsActive);
    }

    // ═══════════════════════════════════════════════════════════
    //  BATTLE PASS METHODS
    // ═══════════════════════════════════════════════════════════

    public async Task<BattlePassProgress> GetOrCreatePassAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<BattlePassConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        var season = config?.CurrentSeason ?? 1;

        var progress = await ctx.GetTable<BattlePassProgress>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && x.Season == season);

        if (progress is not null) return progress;

        progress = new BattlePassProgress { UserId = userId, GuildId = guildId, Season = season };
        ctx.Add(progress);
        await ctx.SaveChangesAsync();
        return progress;
    }

    public async Task<(int NewTier, bool LeveledUp)> AddBattlePassXpAsync(ulong userId, ulong guildId, long xp)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<BattlePassConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null || !config.IsActive) return (0, false);

        var progress = await GetOrCreatePassAsync(userId, guildId);
        var oldTier = progress.CurrentTier;
        progress.SeasonXp += xp;

        var xpPerTier = config.XpPerTier;
        var newTier = (int)(progress.SeasonXp / xpPerTier) + 1;
        newTier = Math.Min(newTier, config.MaxTier);

        await ctx.GetTable<BattlePassProgress>()
            .Where(x => x.Id == progress.Id)
            .UpdateAsync(_ => new BattlePassProgress
            {
                SeasonXp = progress.SeasonXp,
                CurrentTier = newTier,
            });

        return (newTier, newTier > oldTier);
    }

    public async Task<BattlePassConfig> GetOrCreateConfigAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<BattlePassConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null) return config;

        config = new BattlePassConfig { GuildId = guildId };
        ctx.Add(config);
        await ctx.SaveChangesAsync();
        return config;
    }

    public static string RarityEmoji(string rarity) => rarity switch
    {
        "Common" => "⬜", "Uncommon" => "🟩", "Rare" => "🟦",
        "Epic" => "🟪", "Legendary" => "🟧", _ => "⬜",
    };
}
