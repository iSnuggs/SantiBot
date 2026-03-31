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

    public static readonly List<(string Id, string Name, string Desc, string Emoji, string Category)> AllAchievements = new()
    {
        // ═══════════════════════════════════════════
        //  MESSAGING (1-30)
        // ═══════════════════════════════════════════
        ("first_msg",        "First Message",       "Send your first message",              "💬", "Messaging"),
        ("msg_100",          "Chatterbox",          "Send 100 messages",                    "📣", "Messaging"),
        ("msg_500",          "Talkative",           "Send 500 messages",                    "🗣️", "Messaging"),
        ("msg_1000",         "Keyboard Warrior",    "Send 1,000 messages",                  "⌨️", "Messaging"),
        ("msg_5000",         "Motor Mouth",         "Send 5,000 messages",                  "💨", "Messaging"),
        ("msg_10000",        "Legendary Talker",    "Send 10,000 messages",                 "🏆", "Messaging"),
        ("msg_25000",        "Unstoppable",         "Send 25,000 messages",                 "🌪️", "Messaging"),
        ("msg_50000",        "Living Legend",        "Send 50,000 messages",                "👑", "Messaging"),
        ("msg_100000",       "Server Historian",    "Send 100,000 messages",                "📜", "Messaging"),
        ("night_owl",        "Night Owl",           "Send a message after midnight",        "🦉", "Messaging"),
        ("early_bird",       "Early Bird",          "Send a message before 6 AM",           "🐦", "Messaging"),
        ("weekend_warrior",  "Weekend Warrior",     "Be active on a weekend",               "🎮", "Messaging"),
        ("monday_grind",     "Monday Grinder",      "Be active on a Monday",                "😤", "Messaging"),
        ("friday_hype",      "Friday Hype",         "Send 50+ messages on a Friday",        "🎉", "Messaging"),
        ("emoji_lover",      "Emoji Lover",         "Use 500 emojis in messages",           "😍", "Messaging"),
        ("long_msg",         "Essayist",            "Send a message over 2000 characters",  "📝", "Messaging"),
        ("short_king",       "Short King",          "Send 1000 messages under 5 characters","👑", "Messaging"),
        ("link_sharer",      "Link Sharer",         "Share 100 links",                      "🔗", "Messaging"),
        ("image_poster",     "Shutterbug",          "Post 100 images",                      "📸", "Messaging"),
        ("reply_king",       "Reply King",          "Reply to 500 messages",                "↩️", "Messaging"),
        ("thread_starter",   "Thread Starter",      "Create 10 threads",                    "🧵", "Messaging"),
        ("reaction_giver",   "Reaction Machine",    "Add 1000 reactions",                   "👍", "Messaging"),
        ("reaction_magnet",  "Reaction Magnet",     "Get 500 reactions on your messages",   "🧲", "Messaging"),
        ("first_in_channel", "Pioneer",             "Be the first to post in a new channel","🏁", "Messaging"),
        ("msg_all_channels", "Explorer",            "Post in every text channel",           "🗺️", "Messaging"),
        ("midnight_msg",     "Midnight Messenger",  "Send a message at exactly midnight",   "🕛", "Messaging"),
        ("new_year_msg",     "New Year Chatter",    "Send a message on January 1st",        "🎆", "Messaging"),
        ("halloween_msg",    "Spooky Speaker",      "Send a message on Halloween",          "🎃", "Messaging"),
        ("xmas_msg",         "Holiday Spirit",      "Send a message on Christmas",          "🎄", "Messaging"),
        ("valentines_msg",   "Lovebird",            "Send a message on Valentine's Day",    "💕", "Messaging"),

        // ═══════════════════════════════════════════
        //  STREAKS & CONSISTENCY (31-55)
        // ═══════════════════════════════════════════
        ("streak_3",         "Getting Started",     "Be active 3 days in a row",            "🔥", "Streaks"),
        ("streak_7",         "7-Day Streak",        "Be active 7 days in a row",            "🔥", "Streaks"),
        ("streak_14",        "Two Weeks Strong",    "Be active 14 days in a row",           "💪", "Streaks"),
        ("streak_30",        "Monthly Devotion",    "Be active 30 days in a row",           "📅", "Streaks"),
        ("streak_60",        "Iron Will",           "Be active 60 days in a row",           "🦾", "Streaks"),
        ("streak_90",        "Quarterly Beast",     "Be active 90 days in a row",           "🏋️", "Streaks"),
        ("streak_180",       "Half Year Hero",      "Be active 180 days in a row",          "⚡", "Streaks"),
        ("streak_365",       "Year-Round Legend",   "Be active 365 days in a row",          "🌟", "Streaks"),
        ("daily_50",         "Daily Collector",     "Claim 50 daily rewards",               "💰", "Streaks"),
        ("daily_100",        "Daily Devotee",       "Claim 100 daily rewards",              "💎", "Streaks"),
        ("daily_365",        "Year of Dailies",     "Claim 365 daily rewards",              "🗓️", "Streaks"),
        ("active_months_3",  "Regular",             "Be active for 3 months",               "📊", "Streaks"),
        ("active_months_6",  "Veteran",             "Be active for 6 months",               "🎖️", "Streaks"),
        ("active_months_12", "Annual Member",       "Be active for 12 months",              "🏅", "Streaks"),
        ("comeback",         "The Comeback",        "Return after 30+ days away",           "🔄", "Streaks"),
        ("all_day_active",   "24/7",                "Send messages in 24 different hours",  "⏰", "Streaks"),
        ("every_day_of_week","Full Week",           "Be active on every day of the week",   "📆", "Streaks"),
        ("first_of_month",   "First of the Month",  "Be first to post on a new month",     "1️⃣", "Streaks"),
        ("last_of_month",    "Last Word",           "Send the last message of a month",     "🔚", "Streaks"),
        ("triple_streak",    "Triple Threat",       "Have 3 streaks active at once",        "3️⃣", "Streaks"),
        ("streak_saved",     "Close Call",          "Continue a streak with <1hr to spare",  "😰", "Streaks"),
        ("holiday_streak",   "Holiday Grinder",     "Maintain streak through a holiday",    "🎄", "Streaks"),
        ("birthday_active",  "Birthday Active",     "Be active on your birthday",           "🎂", "Streaks"),
        ("no_break",         "Unbreakable",         "Never break a streak after 30 days",   "💎", "Streaks"),
        ("speed_daily",      "Speed Collector",     "Claim daily within 1 min of reset",    "⚡", "Streaks"),

        // ═══════════════════════════════════════════
        //  SOCIAL & RELATIONSHIPS (56-90)
        // ═══════════════════════════════════════════
        ("karma_10",         "Well Liked",          "Reach 10 karma",                       "⭐", "Social"),
        ("karma_50",         "Popular",             "Reach 50 karma",                       "🌟", "Social"),
        ("karma_100",        "Community Favorite",  "Reach 100 karma",                      "💫", "Social"),
        ("karma_500",        "Beloved",             "Reach 500 karma",                      "❤️", "Social"),
        ("karma_1000",       "Server Saint",        "Reach 1000 karma",                     "😇", "Social"),
        ("married",          "Taken",               "Get married",                          "💍", "Social"),
        ("divorced",         "Uncoupled",           "Get divorced",                         "💔", "Social"),
        ("remarried",        "Second Chance",       "Get married after a divorce",          "💞", "Social"),
        ("friends_5",        "Social Butterfly",    "Have 5 friends",                       "🦋", "Social"),
        ("friends_10",       "Popular Kid",         "Have 10 friends",                      "🤝", "Social"),
        ("friends_25",       "Center of Attention", "Have 25 friends",                      "🌐", "Social"),
        ("friends_50",       "Networking God",      "Have 50 friends",                      "👥", "Social"),
        ("adopted",          "Found Family",        "Get adopted by someone",               "🏠", "Social"),
        ("adopter",          "Parental Figure",     "Adopt someone",                        "👪", "Social"),
        ("big_family",       "Big Family",          "Have 5+ family members",               "👨‍👩‍👧‍👦", "Social"),
        ("compliment_10",    "Kind Soul",           "Give 10 compliments",                  "😊", "Social"),
        ("compliment_100",   "Compliment King",     "Give 100 compliments",                 "👑", "Social"),
        ("hugs_100",         "Hugger",              "Give 100 hugs",                        "🤗", "Social"),
        ("pat_100",          "Head Patter",         "Give 100 headpats",                    "✋", "Social"),
        ("rep_given_50",     "Voucher",             "Give 50 reputation points",            "👍", "Social"),
        ("rep_received_50",  "Trusted",             "Receive 50 reputation points",         "🛡️", "Social"),
        ("bio_set",          "About Me",            "Set your bio for the first time",      "📋", "Social"),
        ("mood_set_30",      "Mood Logger",         "Log your mood 30 times",               "😊", "Social"),
        ("time_capsule",     "Time Traveler",       "Open a time capsule",                  "⏳", "Social"),
        ("compatibility_100","Match Maker",         "Check compatibility 100 times",        "💘", "Social"),
        ("confession",       "Confessor",           "Submit an anonymous confession",       "🤫", "Social"),
        ("greeter_10",       "Welcomer",            "Welcome 10 new members",               "👋", "Social"),
        ("helped_10",        "Helpful",             "Get marked as helpful 10 times",       "🆘", "Social"),
        ("mentor",           "Mentor",              "Help 50 users",                        "🎓", "Social"),
        ("server_og",        "OG",                  "Be one of the first 50 members",       "🏛️", "Social"),
        ("boosted",          "Booster",             "Boost the server",                     "💎", "Social"),
        ("invite_5",         "Recruiter",           "Invite 5 members who stayed",          "📨", "Social"),
        ("invite_25",        "Ambassador",          "Invite 25 members who stayed",         "🏳️", "Social"),
        ("suggestion_made",  "Idea Person",         "Submit a suggestion",                  "💡", "Social"),
        ("poll_voted_20",    "Voter",               "Vote in 20 polls",                     "🗳️", "Social"),

        // ═══════════════════════════════════════════
        //  VOICE CHAT (91-115)
        // ═══════════════════════════════════════════
        ("voice_1h",         "Voice Regular",       "Spend 1 hour in voice",                "🎤", "Voice"),
        ("voice_10h",        "Talkative",           "Spend 10 hours in voice",              "🔊", "Voice"),
        ("voice_24h",        "Voice Addict",        "Spend 24 hours in voice",              "🎙️", "Voice"),
        ("voice_100h",       "Voice Junkie",        "Spend 100 hours in voice",             "📢", "Voice"),
        ("voice_500h",       "Professional Talker", "Spend 500 hours in voice",             "🏆", "Voice"),
        ("voice_1000h",      "Voice God",           "Spend 1000 hours in voice",            "👑", "Voice"),
        ("voice_midnight",   "Late Night Talks",    "Be in voice after midnight",           "🌙", "Voice"),
        ("voice_marathon",   "Voice Marathon",      "Stay in voice for 8+ hours straight",  "🏃", "Voice"),
        ("voice_5_people",   "Party Time",          "Be in voice with 5+ people",           "🎊", "Voice"),
        ("voice_10_people",  "Full House",          "Be in voice with 10+ people",          "🏠", "Voice"),
        ("voice_all_channels","Channel Hopper",     "Join every voice channel",             "🔀", "Voice"),
        ("stream_1h",        "Streamer",            "Stream for 1 hour in voice",           "📺", "Voice"),
        ("stream_10h",       "Content Creator",     "Stream for 10 hours total",            "🎬", "Voice"),
        ("deafened_1h",      "Observer",            "Sit deafened for 1 hour",              "🔇", "Voice"),
        ("muted_meeting",    "On Mute",             "Stay muted for an entire session",     "🤐", "Voice"),
        ("voice_every_day",  "Voice Daily",         "Join voice every day for a week",      "📅", "Voice"),
        ("music_listener",   "Music Fan",           "Listen to 100 songs via bot",          "🎵", "Voice"),
        ("music_500",        "Audiophile",          "Listen to 500 songs via bot",          "🎶", "Voice"),
        ("dj_session",       "DJ",                  "Queue 20 songs in one session",        "🎧", "Voice"),
        ("voice_alone",      "Soliloquy",           "Sit in voice alone for 30 minutes",    "😶", "Voice"),
        ("karaoke",          "Karaoke Star",        "Use karaoke mode",                     "🎤", "Voice"),
        ("soundboard_50",    "Sound Effect Pro",    "Play 50 sound effects",                "🔔", "Voice"),
        ("first_in_voice",   "Voice Pioneer",       "Be first in voice channel today",      "🏁", "Voice"),
        ("voice_partner",    "Voice Buddy",         "Spend 50+ hours with same person",     "👫", "Voice"),
        ("stage_speaker",    "Stage Speaker",       "Speak on a stage channel",             "🎭", "Voice"),

        // ═══════════════════════════════════════════
        //  XP & LEVELING (116-150)
        // ═══════════════════════════════════════════
        ("level_5",          "Newbie No More",      "Reach level 5",                        "📈", "Leveling"),
        ("level_10",         "Getting There",       "Reach level 10",                       "📊", "Leveling"),
        ("level_25",         "Experienced",         "Reach level 25",                       "🌟", "Leveling"),
        ("level_50",         "Seasoned",            "Reach level 50",                       "💫", "Leveling"),
        ("level_75",         "Elite",               "Reach level 75",                       "⚡", "Leveling"),
        ("level_100",        "Centurion",           "Reach level 100",                      "💯", "Leveling"),
        ("level_150",        "Legendary",           "Reach level 150",                      "🏅", "Leveling"),
        ("level_200",        "Mythical",            "Reach level 200",                      "🔱", "Leveling"),
        ("prestige_1",       "Prestige I",          "Prestige for the first time",          "⭐", "Leveling"),
        ("prestige_2",       "Prestige II",         "Prestige twice",                       "⭐", "Leveling"),
        ("prestige_5",       "Prestige V",          "Prestige five times",                  "🌟", "Leveling"),
        ("prestige_10",      "Prestige X",          "Prestige ten times",                   "💫", "Leveling"),
        ("xp_top_daily",     "Daily Champion",      "Be #1 in XP for a day",               "🥇", "Leveling"),
        ("xp_top_weekly",    "Weekly Champion",     "Be #1 in XP for a week",              "🏆", "Leveling"),
        ("xp_top_monthly",   "Monthly Champion",    "Be #1 in XP for a month",             "👑", "Leveling"),
        ("xp_booster_used",  "Boosted",             "Use an XP booster",                    "🚀", "Leveling"),
        ("xp_challenge_done","Challenge Complete",  "Complete an XP challenge",             "✅", "Leveling"),
        ("xp_10_challenges", "Challenger",          "Complete 10 XP challenges",            "🎯", "Leveling"),
        ("xp_season_top10",  "Season Elite",        "Finish in top 10 of a season",         "🏅", "Leveling"),
        ("xp_team_win",      "Team Player",         "Win a team XP competition",            "🤝", "Leveling"),
        ("fast_level",       "Speed Leveler",       "Level up twice in one day",            "⚡", "Leveling"),
        ("level_up_voice",   "Voice Level",         "Level up from voice XP alone",         "🔊", "Leveling"),
        ("max_xp_day",       "XP Maxer",            "Hit the daily XP cap",                 "📈", "Leveling"),
        ("xp_gifted",        "Generous",            "Gift XP to someone",                   "🎁", "Leveling"),
        ("xp_received",      "Appreciated",         "Receive gifted XP",                    "🎀", "Leveling"),
        ("leaderboard_climb","Climber",             "Move up 10+ spots on leaderboard",     "🧗", "Leveling"),
        ("leaderboard_top3", "Podium Finish",       "Reach top 3 on the leaderboard",       "🥉", "Leveling"),
        ("leaderboard_1",    "Number One",          "Reach #1 on the leaderboard",          "🥇", "Leveling"),
        ("xp_from_all",      "Well-Rounded",        "Earn XP from text, voice, and events", "🔄", "Leveling"),
        ("level_palindrome", "Palindrome",          "Reach a palindrome level (11,22,33..)", "🪞", "Leveling"),
        ("level_69",         "Nice",                "Reach level 69",                       "😏", "Leveling"),
        ("level_420",        "Dank",                "Reach level 420",                      "🌿", "Leveling"),
        ("xp_million",       "Millionaire XP",      "Accumulate 1,000,000 total XP",       "💎", "Leveling"),
        ("all_role_rewards",  "Role Collector",     "Earn every level-based role",          "🎭", "Leveling"),
        ("xp_no_boost",      "Purist",              "Reach level 50 without XP boosters",   "🧘", "Leveling"),

        // ═══════════════════════════════════════════
        //  ECONOMY & CURRENCY (151-200)
        // ═══════════════════════════════════════════
        ("first_currency",   "First Coins",         "Earn your first currency",             "🪙", "Economy"),
        ("currency_1000",    "Stacked",             "Have 1,000 currency at once",          "💰", "Economy"),
        ("currency_10000",   "Wealthy",             "Have 10,000 currency at once",         "💎", "Economy"),
        ("currency_100000",  "Rich",                "Have 100,000 currency at once",        "🤑", "Economy"),
        ("currency_1m",      "Millionaire",         "Have 1,000,000 currency at once",      "👑", "Economy"),
        ("currency_10m",     "Multi-Millionaire",   "Have 10,000,000 currency at once",     "🏰", "Economy"),
        ("gamble_win",       "Lucky",               "Win a gamble",                         "🎰", "Economy"),
        ("gamble_win_10",    "On a Roll",           "Win 10 gambles",                       "🎲", "Economy"),
        ("gamble_win_100",   "High Roller",         "Win 100 gambles",                      "💵", "Economy"),
        ("gamble_jackpot",   "JACKPOT",             "Hit a jackpot",                        "🎰", "Economy"),
        ("gamble_lose_all",  "Broke",               "Lose all your currency gambling",      "😭", "Economy"),
        ("bet_1000",         "Big Spender",         "Bet 1000+ in a single gamble",         "💸", "Economy"),
        ("bet_10000",        "Whale",               "Bet 10,000+ in a single gamble",       "🐋", "Economy"),
        ("shop_buy_10",      "Shopper",             "Buy 10 items from the shop",           "🛒", "Economy"),
        ("shop_buy_100",     "Shopaholic",          "Buy 100 items from the shop",          "🛍️", "Economy"),
        ("trade_10",         "Trader",              "Complete 10 trades",                   "🤝", "Economy"),
        ("trade_100",        "Merchant",            "Complete 100 trades",                  "⚖️", "Economy"),
        ("bounty_claimed",   "Bounty Hunter",       "Claim a bounty",                      "🎯", "Economy"),
        ("heist_success",    "Heist Master",        "Complete a heist successfully",        "🏦", "Economy"),
        ("heist_5",          "Crime Syndicate",     "Complete 5 heists",                    "🕶️", "Economy"),
        ("lottery_win",      "Lottery Winner",       "Win the lottery",                     "🎫", "Economy"),
        ("stock_profit",     "Wolf of Wall Street", "Make 10,000 profit on stocks",         "📈", "Economy"),
        ("crypto_moon",      "To the Moon",         "10x a crypto investment",              "🌙", "Economy"),
        ("property_owned",   "Homeowner",           "Buy your first property",              "🏠", "Economy"),
        ("business_opened",  "Entrepreneur",        "Open a business",                      "🏢", "Economy"),
        ("loan_paid",        "Debt Free",           "Pay off a loan",                       "✅", "Economy"),
        ("tax_paid",         "Good Citizen",        "Pay your taxes",                       "🏛️", "Economy"),
        ("auction_won",      "Auction Victor",      "Win an auction",                       "🔨", "Economy"),
        ("lootbox_100",      "Lootbox Addict",      "Open 100 lootboxes",                  "📦", "Economy"),
        ("lootbox_legendary","Golden Lootbox",      "Get a legendary from a lootbox",       "🌟", "Economy"),
        ("fishing_100",      "Fisher",              "Catch 100 fish",                       "🐟", "Economy"),
        ("fishing_legendary","Legendary Catch",     "Catch a legendary fish",               "🐋", "Economy"),
        ("crafting_100",     "Artisan",             "Craft 100 items",                      "🔨", "Economy"),
        ("crafting_legendary","Master Crafter",     "Craft a legendary item",               "⚒️", "Economy"),
        ("give_currency",    "Philanthropist",      "Give away 10,000 total currency",      "🎁", "Economy"),
        ("richest_ever",     "Peak Wealth",         "Hold the server wealth record",        "🏆", "Economy"),
        ("rags_to_riches",   "Rags to Riches",      "Go from 0 to 10,000 in one day",     "📈", "Economy"),
        ("daily_all_bonuses","Bonus Collector",     "Claim all daily bonuses in one day",   "✨", "Economy"),
        ("job_10",           "Hard Worker",         "Complete 10 jobs",                     "💼", "Economy"),
        ("job_100",          "Career Person",       "Complete 100 jobs",                    "👔", "Economy"),
        ("shop_owner",       "Shop Owner",          "Open your own shop",                   "🏪", "Economy"),
        ("currency_spent_1m","Big Spender Total",   "Spend 1,000,000 total currency",       "💸", "Economy"),
        ("invest_profit_100k","Investment Guru",    "Make 100,000 total investment profit",  "📊", "Economy"),
        ("farm_harvest_100", "Farmer",              "Harvest 100 crops",                    "🌾", "Economy"),
        ("mine_100",         "Miner",               "Mine 100 ores",                        "⛏️", "Economy"),
        ("cook_50",          "Chef",                "Cook 50 meals",                        "👨‍🍳", "Economy"),
        ("alchemist_50",     "Alchemist",           "Brew 50 potions",                      "🧪", "Economy"),
        ("enchant_20",       "Enchanter",           "Enchant 20 items",                     "✨", "Economy"),
        ("blacksmith_50",    "Blacksmith",          "Forge 50 weapons/armor",               "🔥", "Economy"),
        ("zero_to_hero",     "Zero to Hero",        "Go from 0 currency to top 10",         "🦸", "Economy"),

        // ═══════════════════════════════════════════
        //  DUNGEON & RPG (201-270)
        // ═══════════════════════════════════════════
        ("dungeon_first",    "Adventurer",          "Complete your first dungeon",           "🏰", "Dungeon"),
        ("dungeon_10",       "Dungeon Runner",      "Complete 10 dungeons",                  "⚔️", "Dungeon"),
        ("dungeon_50",       "Dungeon Veteran",     "Complete 50 dungeons",                  "🗡️", "Dungeon"),
        ("dungeon_100",      "Dungeon Master",      "Complete 100 dungeons",                 "👑", "Dungeon"),
        ("dungeon_500",      "Dungeon Legend",       "Complete 500 dungeons",                "🏆", "Dungeon"),
        ("dungeon_1000",     "Dungeon God",         "Complete 1000 dungeons",                "⚡", "Dungeon"),
        ("dungeon_diff5",    "Max Difficulty",       "Complete a difficulty 5 dungeon",      "💀", "Dungeon"),
        ("dungeon_no_damage","Flawless Run",         "Complete a dungeon without damage",    "✨", "Dungeon"),
        ("dungeon_solo",     "Solo Hero",            "Complete a dungeon alone",             "🦸", "Dungeon"),
        ("dungeon_full_party","Full Squad",          "Complete a dungeon with 4 players",    "👥", "Dungeon"),
        ("monster_kill_100", "Monster Slayer",       "Kill 100 monsters",                   "💀", "Dungeon"),
        ("monster_kill_1000","Monster Hunter",       "Kill 1,000 monsters",                 "🎯", "Dungeon"),
        ("monster_kill_10000","Genocide",            "Kill 10,000 monsters",                "☠️", "Dungeon"),
        ("class_warrior",    "Warrior Class",        "Play as a Warrior",                   "🛡️", "Dungeon"),
        ("class_mage",       "Mage Class",           "Play as a Mage",                     "🔮", "Dungeon"),
        ("class_rogue",      "Rogue Class",          "Play as a Rogue",                    "🗡️", "Dungeon"),
        ("class_cleric",     "Cleric Class",         "Play as a Cleric",                   "✝️", "Dungeon"),
        ("class_monk",       "Monk Class",           "Play as a Monk",                     "👊", "Dungeon"),
        ("class_barbarian",  "Barbarian Class",      "Play as a Barbarian",                "⚔️", "Dungeon"),
        ("class_ranger",     "Ranger Class",         "Play as a Ranger",                   "🏹", "Dungeon"),
        ("class_paladin",    "Paladin Class",        "Play as a Paladin",                  "⚜️", "Dungeon"),
        ("class_bard",       "Bard Class",           "Play as a Bard",                     "🎵", "Dungeon"),
        ("class_necromancer","Necromancer Class",     "Play as a Necromancer",              "💀", "Dungeon"),
        ("class_druid",      "Druid Class",          "Play as a Druid",                    "🌿", "Dungeon"),
        ("all_classes",      "Jack of All Trades",   "Play every class at least once",      "🎭", "Dungeon"),
        ("all_races",        "Worldly",              "Play every race at least once",       "🌍", "Dungeon"),
        ("dungeon_level_10", "Dungeon Lv.10",        "Reach dungeon level 10",             "📈", "Dungeon"),
        ("dungeon_level_25", "Dungeon Lv.25",        "Reach dungeon level 25",             "📊", "Dungeon"),
        ("dungeon_level_50", "Dungeon Lv.50",        "Reach dungeon level 50",             "🌟", "Dungeon"),
        ("dungeon_level_100","Dungeon Lv.100",       "Reach dungeon level 100",            "💯", "Dungeon"),
        ("loot_common",      "Common Collector",     "Find 50 common items",               "⬜", "Dungeon"),
        ("loot_uncommon",    "Uncommon Collector",   "Find 25 uncommon items",              "🟩", "Dungeon"),
        ("loot_rare",        "Rare Collector",       "Find 10 rare items",                  "🟦", "Dungeon"),
        ("loot_epic",        "Epic Collector",       "Find 5 epic items",                   "🟪", "Dungeon"),
        ("loot_legendary",   "Legendary Collector",  "Find a legendary item",               "🟧", "Dungeon"),
        ("equip_full_set",   "Fully Equipped",       "Equip weapon, armor, and accessory",  "🛡️", "Dungeon"),
        ("death_first",      "First Death",          "Die in a dungeon for the first time",  "💀", "Dungeon"),
        ("death_10",         "Danger Prone",         "Die 10 times in dungeons",            "☠️", "Dungeon"),
        ("death_100",        "Immortal Spirit",      "Die 100 times but keep going",         "👻", "Dungeon"),
        ("survived_1hp",     "Clutch",               "Survive a dungeon with 1 HP",         "😰", "Dungeon"),
        ("trap_disarm_10",   "Trap Expert",          "Disarm 10 traps",                     "🔧", "Dungeon"),
        ("treasure_50",      "Treasure Hunter",      "Find 50 treasure rooms",              "💰", "Dungeon"),
        ("spring_heal_20",   "Spring Drinker",       "Heal at 20 healing springs",          "✨", "Dungeon"),
        ("skeleton_raised",  "Bone Commander",       "Raise 10 skeletons as Necromancer",   "💀", "Dungeon"),
        ("reckless_survive", "Living Dangerously",   "Survive Reckless Attack at 1 HP",     "⚔️", "Dungeon"),
        ("fireball_kill",    "Pyromaniac",           "Kill 25 monsters with Fireball",       "🔥", "Dungeon"),
        ("sneak_attack_20",  "Shadow Striker",       "Land 20 Sneak Attacks",               "🗡️", "Dungeon"),
        ("heal_party_500",   "Lifesaver",            "Heal 500 total party HP as Cleric",    "💚", "Dungeon"),
        ("tame_monster",     "Beast Tamer",          "Tame a monster as Ranger",            "🏹", "Dungeon"),
        ("wild_shape_10",    "Shapeshifter",         "Use Wild Shape 10 times as Druid",    "🌿", "Dungeon"),
        ("inspire_50",       "Inspiring",            "Inspire 50 allies as Bard",            "🎵", "Dungeon"),
        ("divine_smite_25",  "Holy Avenger",         "Land 25 Divine Smites as Paladin",    "⚜️", "Dungeon"),
        ("flurry_50",        "Combo Master",         "Land 50 Flurry of Blows as Monk",     "👊", "Dungeon"),
        ("party_wipe",       "Total Party Kill",     "Experience a full party wipe",         "💀", "Dungeon"),
        ("speed_clear",      "Speed Runner",         "Clear a dungeon in under 5 minutes",   "⚡", "Dungeon"),
        ("loot_hoarder",     "Loot Hoarder",         "Have 50 items in inventory",          "🎒", "Dungeon"),
        ("xp_pool_10000",    "XP Farm",              "Earn 10,000 XP in a single run",      "📈", "Dungeon"),
        ("boss_rush",        "Boss Rush",            "Fight 5 bosses in a row",              "👹", "Dungeon"),
        ("flee_success",     "Tactical Retreat",     "Successfully flee 10 times",           "🏃", "Dungeon"),
        ("flee_fail_death",  "Bad Escape Plan",      "Die from a failed flee attempt",       "😵", "Dungeon"),
        ("all_monsters",     "Bestiary Complete",    "Encounter every monster type",          "📖", "Dungeon"),
        ("dungeon_millionaire","Dungeon Millionaire","Earn 1,000,000 loot from dungeons",    "💎", "Dungeon"),
        ("phoenix_save",     "Phoenix Rising",       "Be saved by Phoenix Feather",          "🔥", "Dungeon"),
        ("death_ward_save",  "Cheated Death",        "Be saved by Death Ward",               "💀", "Dungeon"),
        ("second_wind",      "Second Wind",          "Proc Second Wind as Warrior",          "🛡️", "Dungeon"),
        ("relentless",       "Relentless",           "Survive lethal hit as Barbarian",      "⚔️", "Dungeon"),
        ("lucky_dodge",      "Lucky Dodge",          "Dodge with Halfling's Lucky",          "🍀", "Dungeon"),
        ("breath_weapon",    "Breath of Fire",       "Trigger Breath Weapon as Dragonborn",  "🐲", "Dungeon"),
        ("infernal_wrath",   "Infernal Fury",        "Trigger Infernal Wrath as Tiefling",   "😈", "Dungeon"),

        // ═══════════════════════════════════════════
        //  RAID BOSSES (271-310)
        // ═══════════════════════════════════════════
        ("raid_first",       "Raid Rookie",          "Participate in your first raid boss",  "⚔️", "Raids"),
        ("raid_10",          "Raid Veteran",         "Participate in 10 raid bosses",        "🗡️", "Raids"),
        ("raid_50",          "Raid Commander",       "Participate in 50 raid bosses",        "🏰", "Raids"),
        ("raid_mvp",         "Raid MVP",             "Deal the most damage in a raid",       "🥇", "Raids"),
        ("raid_mvp_5",       "Raid Legend",          "Be MVP in 5 raids",                    "🏆", "Raids"),
        ("raid_top3",        "Podium Raider",        "Finish top 3 in a raid",              "🥉", "Raids"),
        ("raid_dmg_10000",   "Heavy Hitter",         "Deal 10,000 damage in one raid",      "💥", "Raids"),
        ("raid_dmg_100000",  "Devastator",           "Deal 100,000 damage in one raid",     "💫", "Raids"),
        ("raid_total_1m",    "Million Damage Club",  "Deal 1,000,000 total raid damage",    "🌟", "Raids"),
        ("raid_all_bosses",  "Boss Encyclopedia",    "Fight every type of raid boss",        "📖", "Raids"),
        ("raid_dragon",      "Dragon Slayer",        "Defeat the Infernal Dragon",           "🐉", "Raids"),
        ("raid_lich",        "Lich Bane",            "Defeat the Lich Emperor",              "👑", "Raids"),
        ("raid_kraken",      "Kraken Killer",        "Defeat the Kraken of the Deep",        "🦑", "Raids"),
        ("raid_world_eater", "World Savior",         "Defeat the World Eater",               "🌑", "Raids"),
        ("raid_titan",       "Titan Toppler",        "Defeat the Titan Golem",               "🗿", "Raids"),
        ("raid_phoenix",     "Phoenix Slayer",       "Defeat the Phoenix Overlord",          "🔥", "Raids"),
        ("raid_hydra",       "Hydra Hunter",         "Defeat the Shadow Hydra",              "🐍", "Raids"),
        ("raid_demon",       "Demon Vanquisher",     "Defeat Demon Lord Azaroth",            "😈", "Raids"),
        ("raid_wyrm",        "Wyrm Wrecker",         "Defeat the Frost Wyrm",               "❄️", "Raids"),
        ("raid_behemoth",    "Behemoth Bane",        "Defeat the Abyssal Behemoth",          "👾", "Raids"),
        ("raid_loot_epic",   "Raid Loot: Epic",      "Get an Epic drop from a raid boss",    "🟪", "Raids"),
        ("raid_loot_legendary","Raid Loot: Legendary","Get a Legendary drop from a raid",    "🟧", "Raids"),
        ("raid_phase4",      "Death Throes",         "Fight a boss in phase 4",              "💀", "Raids"),
        ("raid_first_hit",   "First Blood",          "Land the first hit on a raid boss",    "🩸", "Raids"),
        ("raid_killing_blow", "Finishing Blow",       "Land the killing blow on a raid boss", "⚡", "Raids"),
        ("raid_random_spawn","Surprise Boss",         "Be online when a random boss spawns", "🚨", "Raids"),
        ("raid_speed_kill",  "Blitz Kill",            "Help kill a boss in under 10 minutes","⏱️", "Raids"),
        ("raid_marathon",    "Raid Marathon",         "Fight a boss for over 1 hour",        "🏃", "Raids"),
        ("raid_comeback",    "Raid Comeback",         "Kill a boss after it reached phase 4","🔄", "Raids"),
        ("raid_solo_10pct",  "Carry",                 "Deal 10%+ of a boss's total HP alone","🦸", "Raids"),
        ("raid_100_hits",    "Relentless Raider",     "Hit 100 times in a single raid",     "🔨", "Raids"),
        ("raid_all_classes", "Versatile Raider",      "Raid with every class",              "🎭", "Raids"),
        ("raid_streak_5",    "Raid Streak",           "Participate in 5 raids in a row",    "🔥", "Raids"),
        ("raid_dmg_total_10m","Ten Million Club",     "Deal 10,000,000 total raid damage",  "💎", "Raids"),
        ("raid_leaderboard_1","#1 Raider",            "Reach #1 on raid leaderboard",       "👑", "Raids"),
        ("raid_loot_5",      "Loot Hoarder",          "Get 5 items from raid bosses",       "📦", "Raids"),
        ("raid_loot_20",     "Raid Collector",         "Get 20 items from raid bosses",     "🎒", "Raids"),
        ("raid_deathless",   "Deathless Raider",       "Kill a raid boss without dying",    "✨", "Raids"),
        ("raid_underdog",    "Underdog",                "Deal top damage as lowest level",  "🐕", "Raids"),
        ("raid_duo",         "Dynamic Duo",             "Kill a boss with only 2 participants","👯", "Raids"),

        // ═══════════════════════════════════════════
        //  GAMES & MINI-GAMES (311-370)
        // ═══════════════════════════════════════════
        ("trivia_correct_10",  "Trivia Buff",        "Answer 10 trivia questions correctly",  "🧠", "Games"),
        ("trivia_correct_100", "Trivia Master",      "Answer 100 trivia questions correctly",  "🎓", "Games"),
        ("trivia_correct_1000","Trivia God",         "Answer 1000 trivia questions correctly",  "👑", "Games"),
        ("trivia_streak_10",   "Trivia Streak",      "Answer 10 in a row correctly",          "🔥", "Games"),
        ("trivia_perfect",     "Perfect Round",       "Get every question right in a round",  "💯", "Games"),
        ("ttt_win",            "Tic-Tac-Toe Win",    "Win a game of Tic-Tac-Toe",             "❌", "Games"),
        ("ttt_win_10",         "Tic-Tac-Toe Pro",    "Win 10 Tic-Tac-Toe games",              "⭕", "Games"),
        ("c4_win",             "Connect 4 Win",      "Win a game of Connect Four",             "🔴", "Games"),
        ("c4_win_10",          "Connect 4 Pro",      "Win 10 Connect Four games",              "🟡", "Games"),
        ("hangman_win",        "Word Wizard",        "Win a game of Hangman",                  "📝", "Games"),
        ("hangman_win_20",     "Hangman Pro",        "Win 20 Hangman games",                   "🔤", "Games"),
        ("chess_win",          "Chess Victor",       "Win a chess game",                        "♟️", "Games"),
        ("chess_win_10",       "Chess Master",       "Win 10 chess games",                      "♔", "Games"),
        ("pokemon_catch",      "Pokemon Trainer",    "Catch your first Pokemon",                "⚡", "Games"),
        ("pokemon_catch_50",   "Pokemon Collector",  "Catch 50 Pokemon",                        "🎴", "Games"),
        ("pokemon_catch_151",  "Pokedex Complete",   "Catch 151 Pokemon",                       "📕", "Games"),
        ("pokemon_battle_win", "Pokemon Champion",   "Win a Pokemon battle",                    "🏆", "Games"),
        ("battle_royale_win",  "Last One Standing",  "Win a Battle Royale",                     "🏆", "Games"),
        ("battle_royale_5",    "BR Veteran",         "Win 5 Battle Royales",                    "⚔️", "Games"),
        ("hunger_games_win",   "Hunger Games Victor","Survive the Hunger Games",                "🏹", "Games"),
        ("mafia_win",          "Mafia Boss",         "Win a game of Mafia",                     "🕶️", "Games"),
        ("word_chain_100",     "Word Chainer",       "Play 100 words in Word Chain",            "🔗", "Games"),
        ("counting_1000",      "Counter",            "Count to 1000 in counting channel",       "🔢", "Games"),
        ("speed_typing_100",   "Speed Typer",        "Type 100+ WPM in speed typing",           "⌨️", "Games"),
        ("racing_win",         "Speed Racer",        "Win a race",                               "🏎️", "Games"),
        ("racing_win_10",      "Racing Champion",    "Win 10 races",                             "🏁", "Games"),
        ("idle_level_50",      "Idle Master",        "Reach level 50 in Idle Clicker",           "🖱️", "Games"),
        ("tower_wave_50",      "Tower Defense Pro",  "Survive 50 waves in Tower Defense",        "🗼", "Games"),
        ("puzzle_solved_25",   "Puzzle Solver",      "Solve 25 puzzles",                         "🧩", "Games"),
        ("card_collected_50",  "Card Collector",     "Collect 50 unique cards",                   "🃏", "Games"),
        ("card_collected_all", "Full Collection",    "Collect every card",                        "🏆", "Games"),
        ("story_complete",     "Story Complete",     "Finish a story quest",                      "📖", "Games"),
        ("acro_win",           "Wordsmith",          "Win an Acrophobia game",                    "📝", "Games"),
        ("heist_win",          "Heist Master",       "Win a heist",                               "💰", "Games"),
        ("fish_100",           "Angler",             "Catch 100 fish",                            "🐟", "Games"),
        ("fish_legendary",     "Legendary Fisher",   "Catch a legendary fish",                    "🐋", "Games"),
        ("craft_100",          "Crafter",            "Craft 100 items",                           "🔨", "Games"),
        ("game_variety",       "Game Hopper",        "Play 10 different mini-games",              "🎮", "Games"),
        ("game_addict",        "Game Addict",        "Play 1000 total mini-games",               "🕹️", "Games"),
        ("ncanvas_pixel",      "Pixel Artist",       "Place a pixel on NCanvas",                  "🎨", "Games"),
        ("ncanvas_100",        "Canvas Regular",     "Place 100 pixels on NCanvas",               "🖌️", "Games"),
        ("tournament_win",     "Tournament Champion","Win a tournament",                          "🏆", "Games"),
        ("pvp_win_10",         "PvP Veteran",        "Win 10 PvP matches across games",          "⚔️", "Games"),
        ("minigame_mashup",    "Mashup Master",      "Complete a Minigame Mashup",               "🎲", "Games"),
        ("nunchi_win",         "Nunchi Winner",       "Win a game of Nunchi",                     "🔢", "Games"),
        ("quest_complete_10",  "Quest Completer",    "Complete 10 quests",                        "📜", "Games"),
        ("quest_complete_100", "Quest Legend",        "Complete 100 quests",                      "🌟", "Games"),
        ("daily_games_3",      "Triple Play",        "Play 3 different games in one day",         "3️⃣", "Games"),
        ("win_streak_5",       "Winning Streak",     "Win 5 games in a row (any type)",          "🔥", "Games"),
        ("lose_streak_10",     "Unlucky",            "Lose 10 games in a row",                   "😢", "Games"),
        ("games_played_all",   "Tried Everything",   "Play every game type at least once",       "🌈", "Games"),
        ("chess_checkmate_5",  "Checkmate King",     "Checkmate opponents 5 times",              "♚", "Games"),
        ("draw_game",          "Draw Artist",         "Draw a game (tie)",                       "🤝", "Games"),
        ("comeback_win",       "Comeback Kid",       "Win a game after being behind",            "🔄", "Games"),
        ("speedrun",           "Speed Runner",        "Complete any game in record time",        "⚡", "Games"),
        ("perfect_game",       "Perfectionist",       "Win without losing a single point",      "💎", "Games"),
        ("first_game",         "First Game",          "Play your first mini-game",              "🎮", "Games"),
        ("games_100",          "Game Centurion",      "Play 100 total games",                   "💯", "Games"),
        ("games_1000",         "Game Addict",         "Play 1000 total games",                  "🕹️", "Games"),
        ("games_weekend",      "Weekend Gamer",       "Play 10 games on a weekend",             "🎮", "Games"),

        // ═══════════════════════════════════════════
        //  MODERATION (371-400)
        // ═══════════════════════════════════════════
        ("mod_first_action",   "First Mod Action",   "Perform your first moderation action",    "🔨", "Moderation"),
        ("mod_warn_10",        "Warner",             "Issue 10 warnings",                        "⚠️", "Moderation"),
        ("mod_warn_100",       "Strict Mod",         "Issue 100 warnings",                       "🚨", "Moderation"),
        ("mod_mute_10",        "Muter",              "Mute 10 users",                            "🔇", "Moderation"),
        ("mod_ban_10",         "Ban Hammer",         "Ban 10 users",                              "🔨", "Moderation"),
        ("mod_ban_100",        "Iron Fist",          "Ban 100 users",                             "⚒️", "Moderation"),
        ("mod_clean_1000",     "Cleanup Crew",       "Delete 1000 messages",                      "🧹", "Moderation"),
        ("mod_ticket_resolve", "Ticket Closer",      "Resolve 10 tickets",                       "🎫", "Moderation"),
        ("mod_ticket_100",     "Ticket Master",      "Resolve 100 tickets",                      "🏆", "Moderation"),
        ("mod_antiraid",       "Raid Blocker",       "Block a raid attempt",                      "🛡️", "Moderation"),
        ("mod_shift_10",       "Active Mod",         "Complete 10 mod shifts",                   "📋", "Moderation"),
        ("mod_report_handle",  "Report Handler",     "Handle 10 user reports",                   "📝", "Moderation"),
        ("mod_appeal_handle",  "Appeal Judge",       "Handle 10 ban appeals",                    "⚖️", "Moderation"),
        ("mod_modmail_50",     "Mod Mail Pro",       "Handle 50 mod mail tickets",                "📬", "Moderation"),
        ("mod_automod_setup",  "Automod Config",     "Set up automod rules",                      "🤖", "Moderation"),
        ("no_warnings",        "Clean Record",       "Go 6 months without a warning",            "✨", "Moderation"),
        ("helpful_report",     "Vigilant",           "Submit a report that leads to action",      "👁️", "Moderation"),
        ("phishing_caught",    "Phishing Spotter",   "Catch a phishing link",                    "🎣", "Moderation"),
        ("nuke_prevented",     "Server Guardian",    "Prevent a nuke attempt",                    "🛡️", "Moderation"),
        ("mod_veteran",        "Mod Veteran",         "Be a moderator for 6+ months",            "🎖️", "Moderation"),
        ("evidence_10",        "Evidence Collector",  "Add 10 evidence items",                   "📎", "Moderation"),
        ("quarantine_5",       "Quarantine Officer",  "Quarantine 5 suspicious accounts",        "🔒", "Moderation"),
        ("mod_note_50",        "Note Taker",          "Write 50 user notes",                     "📝", "Moderation"),
        ("peaceful_day",       "Peaceful Day",        "A day with zero mod actions needed",      "☮️", "Moderation"),
        ("ban_sync_setup",     "Ban Sync",            "Set up cross-server ban sync",            "🔄", "Moderation"),
        ("honeypot_catch",     "Honey Trap",          "Catch someone in a honeypot",             "🍯", "Moderation"),
        ("mod_template_used",  "Template User",       "Use a moderation template",               "📋", "Moderation"),
        ("server_backup",      "Backup Creator",      "Create a server backup",                  "💾", "Moderation"),
        ("permission_fix",     "Permission Fixer",    "Fix a permission issue",                  "🔧", "Moderation"),
        ("mod_100_actions",    "Mod Centurion",       "Perform 100 total mod actions",           "💯", "Moderation"),

        // ═══════════════════════════════════════════
        //  SERVER PARTICIPATION (401-440)
        // ═══════════════════════════════════════════
        ("join_server",        "Welcome",             "Join the server",                          "👋", "Server"),
        ("server_1_month",     "One Month Member",    "Be in the server for 1 month",             "📅", "Server"),
        ("server_3_months",    "Quarter Year",        "Be in the server for 3 months",             "📆", "Server"),
        ("server_6_months",    "Half Year",           "Be in the server for 6 months",             "🗓️", "Server"),
        ("server_1_year",      "One Year",            "Be in the server for 1 year",               "🎂", "Server"),
        ("server_2_years",     "Two Years",           "Be in the server for 2 years",              "🎊", "Server"),
        ("server_3_years",     "Three Years",         "Be in the server for 3 years",              "🏛️", "Server"),
        ("server_5_years",     "Five Year Vet",       "Be in the server for 5 years",              "👑", "Server"),
        ("event_attend",       "Event Goer",          "Attend a server event",                     "🎪", "Server"),
        ("event_attend_10",    "Event Regular",       "Attend 10 server events",                   "🎭", "Server"),
        ("event_attend_50",    "Event Fanatic",       "Attend 50 server events",                   "🌟", "Server"),
        ("event_host",         "Event Host",          "Host a server event",                       "🎤", "Server"),
        ("giveaway_enter",     "Giveaway Entrant",    "Enter a giveaway",                          "🎁", "Server"),
        ("giveaway_win",       "Giveaway Winner",     "Win a giveaway",                            "🏆", "Server"),
        ("starboard",          "Starboard",           "Get a message on the starboard",            "⭐", "Server"),
        ("starboard_10",       "Star Collector",      "Get 10 messages on the starboard",          "🌟", "Server"),
        ("pinned_message",     "Pin-Worthy",          "Get a message pinned",                      "📌", "Server"),
        ("role_10",            "Role Collector",       "Have 10 roles",                            "🎭", "Server"),
        ("custom_profile",     "Profile Designer",     "Customize your profile card",              "🎨", "Server"),
        ("background_bought",  "Background Buyer",    "Buy a profile background",                  "🖼️", "Server"),
        ("all_commands",       "Command Explorer",    "Use 50 different commands",                  "📟", "Server"),
        ("help_used",          "Help Seeker",         "Use the help command",                       "❓", "Server"),
        ("config_changed",     "Server Tweaker",      "Change a server configuration",             "⚙️", "Server"),
        ("bot_invited",        "Bot Inviter",         "Invite SantiBot to a server",               "🤖", "Server"),
        ("feedback_given",     "Feedback Giver",      "Submit feedback or suggestion",              "💡", "Server"),
        ("bug_found",          "Bug Finder",          "Report a bug that gets fixed",               "🐛", "Server"),
        ("emoji_created",      "Emoji Creator",       "Create a server emoji",                     "😀", "Server"),
        ("sticker_created",    "Sticker Creator",     "Create a server sticker",                   "🏷️", "Server"),
        ("thread_popular",     "Thread King",         "Create a thread with 50+ messages",          "🧵", "Server"),
        ("forum_post_popular", "Forum Star",          "Create a forum post with 20+ replies",      "📰", "Server"),
        ("invite_used",        "Recruiter",           "Have someone join using your invite",        "📨", "Server"),
        ("invite_10",          "Top Recruiter",       "Bring 10 members via invite",               "🏅", "Server"),
        ("milestone_member",   "Milestone Member",    "Be the Nth milestone member (100, 500..)",  "🎯", "Server"),
        ("server_anniversary", "Anniversary",         "Be active on server's birthday",            "🎂", "Server"),
        ("nitro_boost",        "Server Booster",      "Boost the server",                          "💎", "Server"),
        ("boost_3_months",     "Loyal Booster",       "Boost for 3+ months",                       "🌟", "Server"),
        ("boost_1_year",       "Annual Booster",      "Boost for 1+ year",                         "👑", "Server"),
        ("every_category",     "Category Explorer",   "Post in every channel category",            "🗺️", "Server"),
        ("slow_mode_patient",  "Patient",             "Post in a slow mode channel 100 times",     "🐢", "Server"),
        ("weekend_event",      "Weekend Warrior",     "Attend a weekend event",                    "🎮", "Server"),

        // ═══════════════════════════════════════════
        //  SECRET / HIDDEN (441-500)
        // ═══════════════════════════════════════════
        ("secret_command",     "???",                 "Find a hidden command",                     "❓", "Secret"),
        ("secret_emoji",       "Secret Emoji",        "Use the secret emoji combination",          "🤫", "Secret"),
        ("egg_hunter",         "Easter Egg Hunter",   "Find an easter egg",                        "🥚", "Secret"),
        ("number_42",          "Meaning of Life",     "Send '42' as a message",                    "🌌", "Secret"),
        ("number_69",          "Nice.",               "Send '69' as a message",                    "😏", "Secret"),
        ("number_420",         "Blazing",             "Send '420' as a message",                   "🌿", "Secret"),
        ("rick_roll",          "Rick Rolled",         "Share a Rick Roll link",                    "🕺", "Secret"),
        ("gg",                 "Good Game",           "Say 'gg' 100 times",                        "🎮", "Secret"),
        ("uwu",               "UwU",                 "Say 'uwu' unironically",                    "😳", "Secret"),
        ("empty_msg",          "Invisible",           "Send a blank-looking message",              "👻", "Secret"),
        ("self_ping",          "Ping Yourself",       "Mention yourself",                          "🔔", "Secret"),
        ("bot_ping",           "Bot Whisperer",       "Mention the bot in chat",                   "🤖", "Secret"),
        ("exactly_midnight",   "Midnight Sharp",      "Send a message at exactly 00:00:00",        "🕛", "Secret"),
        ("same_msg_3x",        "Echo",                "Send the same message 3 times in a row",    "🔊", "Secret"),
        ("palindrome_msg",     "Palindrome",          "Send a palindrome message",                 "🪞", "Secret"),
        ("alphabet_msg",       "Alphabet Soup",       "Send a message with every letter A-Z",      "🔤", "Secret"),
        ("longest_word",       "Wordsmith",           "Use a word over 20 characters",             "📏", "Secret"),
        ("all_caps",           "LOUD",                "Send 10 ALL CAPS messages",                 "📢", "Secret"),
        ("no_vowels",          "Cnsnnt Spkr",         "Send a message with no vowels",             "🤔", "Secret"),
        ("friday_13th",        "Unlucky Day",         "Be active on Friday the 13th",              "🖤", "Secret"),
        ("leap_day",           "Leap Day",            "Be active on February 29th",                "🐸", "Secret"),
        ("pi_day",             "Pi Day",              "Be active on March 14th",                   "🥧", "Secret"),
        ("star_wars_day",      "May the 4th",         "Be active on May 4th",                     "⚔️", "Secret"),
        ("april_fools",        "Fool",                "Be active on April 1st",                    "🤡", "Secret"),
        ("msg_at_level",       "Synced",              "Send a msg when msg count = your level",    "🔄", "Secret"),
        ("all_reactions_1_msg","Reaction Bomb",        "Get 10+ different reactions on one msg",   "💣", "Secret"),
        ("reply_chain_10",     "Reply Chain",         "Be part of a 10+ reply chain",              "🔗", "Secret"),
        ("last_online",        "Night Watch",         "Be the last person active before quiet",    "🌙", "Secret"),
        ("exact_1000",         "Round Number",        "Hit exactly 1000 messages",                 "🎯", "Secret"),
        ("fibonacci",          "Fibonacci",           "Send msg #1, 2, 3, 5, 8, 13, 21 of day",   "🌀", "Secret"),
        ("binary",             "Binary",              "Send a message that's only 0s and 1s",      "🤖", "Secret"),
        ("morse_code",         "Morse Code",          "Send a message in morse code",              "📡", "Secret"),
        ("backwards_msg",      "Mirror Writer",       "Send a message that's a word backwards",    "🔄", "Secret"),
        ("song_lyrics",        "Lyricist",            "Quote song lyrics perfectly",               "🎶", "Secret"),
        ("haiku",              "Haiku Master",         "Send a message in haiku format",           "🌸", "Secret"),
        ("emoji_only",         "Emoji Speaker",       "Send 50 emoji-only messages",               "😀", "Secret"),
        ("thousand_reactions",  "Viral",              "Get 100+ reactions on a single message",    "🔥", "Secret"),
        ("msg_in_1_sec",       "Speed Demon",         "Send 2 messages within 1 second",          "⚡", "Secret"),
        ("exact_69_chars",     "Nice Length",          "Send a message with exactly 69 characters","😏", "Secret"),
        ("msg_id_palindrome",  "ID Palindrome",       "Your message ID is a palindrome",          "🪞", "Secret"),
        ("double_digits",      "Double Digits",        "Level up on a double-digit date (11th, 22nd)","🔢", "Secret"),
        ("all_achievements",   "Completionist",       "Unlock every non-secret achievement",       "🏆", "Secret"),
        ("half_achievements",  "Halfway There",        "Unlock 50% of all achievements",           "🌓", "Secret"),
        ("achievement_hunter", "Achievement Hunter",   "Check achievements 100 times",             "🔍", "Secret"),
        ("help_achievement",   "Meta",                 "Look up the achievement list",             "📋", "Secret"),
        ("first_achievement",  "First Unlock",         "Unlock your very first achievement",       "🔓", "Secret"),
        ("ten_in_one_day",     "Achievement Rush",     "Unlock 10 achievements in one day",        "⚡", "Secret"),
        ("secret_found_all",   "Secret Finder",        "Find all secret achievements",             "🗝️", "Secret"),
        ("lucky_number",       "Lucky Number",         "Be the 777th message of the day",          "🍀", "Secret"),
        ("its_over_9000",      "It's Over 9000!",      "Have any stat exceed 9000",                "💥", "Secret"),
    };

    // Static reference so game services can award achievements without DI
    private static AchievementService _instance;

    public AchievementService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
        _instance = this;
    }

    /// <summary>Award an achievement from any service (static call, fire-and-forget)</summary>
    public static void Award(ulong guildId, ulong userId, string achievementId)
    {
        if (_instance is null) return;
        _ = Task.Run(async () =>
        {
            try { await _instance.TryAwardAsync(guildId, userId, achievementId); }
            catch { /* non-critical */ }
        });
    }

    /// <summary>Award multiple achievements at once</summary>
    public static void AwardMany(ulong guildId, ulong userId, params string[] ids)
    {
        foreach (var id in ids)
            Award(guildId, userId, id);
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
            if (msgCount >= 500)
                await TryAwardAsync(guildId, userId, "msg_500");
            if (msgCount >= 1000)
                await TryAwardAsync(guildId, userId, "msg_1000");
            if (msgCount >= 5000)
                await TryAwardAsync(guildId, userId, "msg_5000");
            if (msgCount >= 10000)
                await TryAwardAsync(guildId, userId, "msg_10000");
            if (msgCount >= 25000)
                await TryAwardAsync(guildId, userId, "msg_25000");
            if (msgCount >= 50000)
                await TryAwardAsync(guildId, userId, "msg_50000");
            if (msgCount >= 100000)
                await TryAwardAsync(guildId, userId, "msg_100000");
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
        if (def.Id is null) return;

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

        // Send achievement notification to configured channel (or system/default)
        try
        {
            var guild = _client.GetGuild(guildId);
            if (guild is not null)
            {
                var user = guild.GetUser(userId);

                // Check for configured achievement channel
                ITextChannel channel = null;
                await using var cfgCtx = _db.GetDbContext();
                var config = await cfgCtx.GetTable<GuildConfig>()
                    .FirstOrDefaultAsyncLinqToDB(g => g.GuildId == guildId);

                if (config?.GameVoiceChannel is not null && config.GameVoiceChannel != 0)
                {
                    // Reuse GameVoiceChannel field for achievement channel (or use a dedicated field if available)
                }

                // Try to find a channel named "achievements" or "bot-log" first
                channel = guild.TextChannels.FirstOrDefault(c => c.Name is "achievements" or "achievement" or "bot-log" or "bot-commands")
                    ?? guild.SystemChannel
                    ?? guild.DefaultChannel as ITextChannel;

                if (channel is not null && user is not null)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("🏆 Achievement Unlocked!")
                        .WithDescription($"{user.Mention} earned **{def.Emoji} {def.Name}**!\n*{def.Desc}*")
                        .WithColor(new Color(0xFFD700))
                        .WithFooter($"Category: {def.Category}")
                        .Build();
                    await channel.SendMessageAsync(embed: embed);
                }
            }
        }
        catch { /* notification is non-critical */ }
    }

    public async Task<List<UserAchievement>> GetAchievementsAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserAchievement>()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<(ulong UserId, int Count)>> GetLeaderboardAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserAchievement>()
            .Where(x => x.GuildId == guildId)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(15)
            .ToListAsyncLinqToDB()
            .ContinueWith(t => t.Result.Select(x => (x.UserId, x.Count)).ToList());
    }
}
