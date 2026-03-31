#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Seasonal;

public sealed class SeasonalService(DbService _db, ICurrencyService _cs) : INService
{
    private const int EGG_HUNT_COOLDOWN_MINUTES = 30;
    private static readonly SantiRandom _rng = new();

    // ─── Seasonal Event Data Model ───────────────────────────────────────
    public sealed class SeasonalEvent
    {
        public string Name { get; init; }
        public string Emoji { get; init; }
        public int StartMonth { get; init; }
        public int StartDay { get; init; }
        public int EndMonth { get; init; }
        public int EndDay { get; init; }
        public string SeasonalCurrency { get; init; }
        public string CurrencyEmoji { get; init; }
        public SeasonalBoss Boss { get; init; }
        public double XpMultiplier { get; init; }
        public double LootMultiplier { get; init; }
        public string Achievement { get; init; }
        public string Greeting { get; init; }
    }

    public sealed class SeasonalBoss
    {
        public string Name { get; init; }
        public string Emoji { get; init; }
        public int Hp { get; init; }
        public int Atk { get; init; }
        public int Def { get; init; }
    }

    // ─── All 8 Seasonal Events ──────────────────────────────────────────
    private static readonly SeasonalEvent[] _events =
    [
        // 1. Valentine's Day (Feb 1-14)
        new()
        {
            Name = "Valentine's Day",
            Emoji = "\u2764\ufe0f",
            StartMonth = 2, StartDay = 1,
            EndMonth = 2, EndDay = 14,
            SeasonalCurrency = "Heart",
            CurrencyEmoji = "\U0001f496",
            Boss = new()
            {
                Name = "Cupid the Heartbreaker",
                Emoji = "\U0001f498",
                Hp = 50000,
                Atk = 320,
                Def = 180
            },
            XpMultiplier = 1.5,
            LootMultiplier = 1.3,
            Achievement = "Love Conquers All",
            Greeting = "Love is in the air! Spread the joy and earn Hearts this Valentine's season!"
        },

        // 2. St. Patrick's Day (Mar 10-17)
        new()
        {
            Name = "St. Patrick's Day",
            Emoji = "\u2618\ufe0f",
            StartMonth = 3, StartDay = 10,
            EndMonth = 3, EndDay = 17,
            SeasonalCurrency = "Gold Coin",
            CurrencyEmoji = "\U0001fa99",
            Boss = new()
            {
                Name = "Leprechaun King O'Malley",
                Emoji = "\U0001f340",
                Hp = 45000,
                Atk = 280,
                Def = 220
            },
            XpMultiplier = 1.4,
            LootMultiplier = 1.7,
            Achievement = "Pot of Gold",
            Greeting = "Top o' the mornin'! The Leprechaun King has hidden gold everywhere. Find it if ye can!"
        },

        // 3. Easter (Apr 1-15)
        new()
        {
            Name = "Easter",
            Emoji = "\U0001f430",
            StartMonth = 4, StartDay = 1,
            EndMonth = 4, EndDay = 15,
            SeasonalCurrency = "Chocolate",
            CurrencyEmoji = "\U0001f36b",
            Boss = new()
            {
                Name = "The Mega Bunny",
                Emoji = "\U0001f407",
                Hp = 40000,
                Atk = 250,
                Def = 200
            },
            XpMultiplier = 1.3,
            LootMultiplier = 1.5,
            Achievement = "Egg-cellent Hunter",
            Greeting = "The Easter Bunny has hidden eggs all over the server! Hunt them down for sweet rewards!"
        },

        // 4. Summer Festival (Jun 15 - Jul 15)
        new()
        {
            Name = "Summer Festival",
            Emoji = "\u2600\ufe0f",
            StartMonth = 6, StartDay = 15,
            EndMonth = 7, EndDay = 15,
            SeasonalCurrency = "Seashell",
            CurrencyEmoji = "\U0001f41a",
            Boss = new()
            {
                Name = "Kraken of the Deep",
                Emoji = "\U0001f419",
                Hp = 75000,
                Atk = 400,
                Def = 250
            },
            XpMultiplier = 1.5,
            LootMultiplier = 1.4,
            Achievement = "Sun-Kissed Champion",
            Greeting = "Summer is here! Hit the beach, ride the waves, and defeat the Kraken lurking in the depths!"
        },

        // 5. Halloween (Oct 15-31)
        new()
        {
            Name = "Halloween",
            Emoji = "\U0001f383",
            StartMonth = 10, StartDay = 15,
            EndMonth = 10, EndDay = 31,
            SeasonalCurrency = "Candy",
            CurrencyEmoji = "\U0001f36c",
            Boss = new()
            {
                Name = "The Headless Horseman",
                Emoji = "\U0001fa78",
                Hp = 66600,
                Atk = 450,
                Def = 300
            },
            XpMultiplier = 1.6,
            LootMultiplier = 1.8,
            Achievement = "Nightmare Slayer",
            Greeting = "The dead walk among us! Venture into the haunted dungeon if you dare... and collect your Candy!"
        },

        // 6. Thanksgiving (Nov 20-30)
        new()
        {
            Name = "Thanksgiving",
            Emoji = "\U0001f983",
            StartMonth = 11, StartDay = 20,
            EndMonth = 11, EndDay = 30,
            SeasonalCurrency = "Feast Token",
            CurrencyEmoji = "\U0001f357",
            Boss = new()
            {
                Name = "Turkeyzilla",
                Emoji = "\U0001f983",
                Hp = 55000,
                Atk = 350,
                Def = 280
            },
            XpMultiplier = 1.4,
            LootMultiplier = 1.5,
            Achievement = "Thankful Champion",
            Greeting = "Gather 'round! Turkeyzilla threatens the harvest feast. Defend the table and earn Feast Tokens!"
        },

        // 7. Christmas (Dec 1-25)
        new()
        {
            Name = "Christmas",
            Emoji = "\U0001f384",
            StartMonth = 12, StartDay = 1,
            EndMonth = 12, EndDay = 25,
            SeasonalCurrency = "Snowflake",
            CurrencyEmoji = "\u2744\ufe0f",
            Boss = new()
            {
                Name = "Krampus the Shadow of Christmas",
                Emoji = "\U0001f608",
                Hp = 80000,
                Atk = 500,
                Def = 350
            },
            XpMultiplier = 2.0,
            LootMultiplier = 2.0,
            Achievement = "Holiday Hero",
            Greeting = "Ho ho ho! Christmas has arrived! Open your advent calendar daily and stop Krampus from ruining the holidays!"
        },

        // 8. New Year (Dec 31 - Jan 2)
        new()
        {
            Name = "New Year",
            Emoji = "\U0001f386",
            StartMonth = 12, StartDay = 31,
            EndMonth = 1, EndDay = 2,
            SeasonalCurrency = "Firework",
            CurrencyEmoji = "\U0001f387",
            Boss = new()
            {
                Name = "Father Time",
                Emoji = "\u23f3",
                Hp = 100000,
                Atk = 550,
                Def = 400
            },
            XpMultiplier = 2.0,
            LootMultiplier = 2.0,
            Achievement = "New Beginning",
            Greeting = "Happy New Year! Double XP is live! Set your resolutions and blast into the new year with fireworks!"
        },
    ];

    // ─── Advent Calendar: 25 Daily Christmas Rewards ─────────────────────
    private static readonly (string reward, int amount, string emoji)[] _adventRewards =
    [
        ("Snowflake",            50,  "\u2744\ufe0f"),   // Day 1
        ("Mystery Box",           1,  "\U0001f381"),     // Day 2
        ("Snowflake",           100,  "\u2744\ufe0f"),   // Day 3
        ("Candy Cane Sword",      1,  "\U0001f36c"),     // Day 4
        ("Snowflake",           150,  "\u2744\ufe0f"),   // Day 5
        ("XP Boost (1hr)",        1,  "\u2b50"),         // Day 6
        ("Snowflake",           200,  "\u2744\ufe0f"),   // Day 7
        ("Reindeer Pet",          1,  "\U0001f98c"),     // Day 8
        ("Snowflake",           250,  "\u2744\ufe0f"),   // Day 9
        ("Gingerbread Shield",    1,  "\U0001f36a"),     // Day 10
        ("Snowflake",           300,  "\u2744\ufe0f"),   // Day 11
        ("Loot Boost (1hr)",      1,  "\U0001f4b0"),     // Day 12
        ("Snowflake",           400,  "\u2744\ufe0f"),   // Day 13
        ("Elf Hat Skin",          1,  "\U0001f3a9"),     // Day 14
        ("Snowflake",           500,  "\u2744\ufe0f"),   // Day 15
        ("Hot Cocoa Potion",      3,  "\u2615"),         // Day 16
        ("Snowflake",           600,  "\u2744\ufe0f"),   // Day 17
        ("Snowman Companion",     1,  "\u26c4"),         // Day 18
        ("Snowflake",           750,  "\u2744\ufe0f"),   // Day 19
        ("Jingle Bell Amulet",    1,  "\U0001f514"),     // Day 20
        ("Snowflake",          1000,  "\u2744\ufe0f"),   // Day 21
        ("Santa's Sleigh Mount",  1,  "\U0001f6f7"),     // Day 22
        ("Snowflake",          1500,  "\u2744\ufe0f"),   // Day 23
        ("Krampus Key",           1,  "\U0001f5dd\ufe0f"), // Day 24
        ("Legendary Gift Box",    1,  "\U0001f381"),     // Day 25
    ];

    // ─── Easter Egg Hunt Data ────────────────────────────────────────────
    private static readonly (string eggName, string emoji, int rarity, long reward)[] _easterEggs =
    [
        ("Painted Egg",        "\U0001f95a", 40, 10),
        ("Golden Egg",         "\U0001fa78", 25, 25),
        ("Chocolate Egg",      "\U0001f36b", 20, 50),
        ("Crystal Egg",        "\U0001f48e", 10, 100),
        ("Legendary Dragon Egg", "\U0001f525",  5, 250),
    ];

    // ─── Service Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the currently active seasonal event, or null if none.
    /// </summary>
    public SeasonalEvent GetActiveEvent()
    {
        var now = DateTime.UtcNow;
        foreach (var ev in _events)
        {
            if (IsDateInRange(now, ev))
                return ev;
        }
        return null;
    }

    /// <summary>
    /// Returns the raid boss for a specific event by name.
    /// </summary>
    public SeasonalBoss GetSeasonalBoss(string eventName)
    {
        var ev = _events.FirstOrDefault(e =>
            e.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase));
        return ev?.Boss;
    }

    /// <summary>
    /// Returns the XP/loot multiplier if an event is active, otherwise 1.0.
    /// </summary>
    public (double xp, double loot) GetSeasonalMultiplier()
    {
        var ev = GetActiveEvent();
        return ev is not null
            ? (ev.XpMultiplier, ev.LootMultiplier)
            : (1.0, 1.0);
    }

    /// <summary>
    /// Check if a specific event is currently active.
    /// </summary>
    public bool IsEventActive(string eventName)
    {
        var now = DateTime.UtcNow;
        var ev = _events.FirstOrDefault(e =>
            e.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase));
        return ev is not null && IsDateInRange(now, ev);
    }

    /// <summary>
    /// Returns all 8 events with their date ranges for the calendar display.
    /// </summary>
    public IReadOnlyList<SeasonalEvent> GetEventCalendar()
        => _events;

    /// <summary>
    /// Returns the advent calendar reward for a given day (1-25). Christmas only.
    /// Each user can only claim each day once.
    /// </summary>
    public async Task<(bool success, string message)> GetAdventCalendarRewardAsync(ulong userId, ulong guildId, int day)
    {
        if (!IsEventActive("Christmas"))
            return (false, "The Advent Calendar is only available during the Christmas event (Dec 1-25)!");

        if (day < 1 || day > 25)
            return (false, "Advent Calendar day must be between 1 and 25!");

        var today = DateTime.UtcNow.Day;
        if (day > today)
            return (false, $"Day {day} hasn't arrived yet! Today is December {today}.");

        // Check if already claimed this day
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<SeasonalClaim>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId
                && x.GuildId == guildId
                && x.ClaimType == "advent"
                && x.Day == day);

        if (existing is not null)
            return (false, $"You already claimed Day {day}! Come back tomorrow for a new reward.");

        // Record the claim
        ctx.Add(new SeasonalClaim
        {
            UserId = userId,
            GuildId = guildId,
            ClaimType = "advent",
            Day = day,
            ClaimedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var (reward, amount, emoji) = _adventRewards[day - 1];

        // Award currency for snowflake rewards
        if (reward == "Snowflake")
            await _cs.AddAsync(userId, amount, new("seasonal", "advent", $"Advent Day {day}"));

        return (true, $"{emoji} **Day {day}:** {amount}x {reward}");
    }

    /// <summary>
    /// Easter egg hunt -- random egg find with currency reward. Easter only.
    /// 30-minute cooldown per user to prevent infinite currency farming.
    /// </summary>
    public async Task<(bool success, string message)> EggHunt(ulong userId, ulong guildId)
    {
        if (!IsEventActive("Easter"))
            return (false, "The Egg Hunt is only available during the Easter event (Apr 1-15)!");

        // Check cooldown
        await using var ctx = _db.GetDbContext();
        var lastClaim = await ctx.GetTable<SeasonalClaim>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.ClaimType == "egghunt")
            .OrderByDescending(x => x.ClaimedAt)
            .FirstOrDefaultAsyncLinqToDB();

        if (lastClaim is not null)
        {
            var cooldownEnd = lastClaim.ClaimedAt.AddMinutes(EGG_HUNT_COOLDOWN_MINUTES);
            if (DateTime.UtcNow < cooldownEnd)
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                return (false, $"You're still looking for eggs! Try again in **{remaining.Minutes}m {remaining.Seconds}s**.");
            }
        }

        // Weighted random egg selection
        var totalWeight = _easterEggs.Sum(e => e.rarity);
        var roll = _rng.Next(totalWeight);
        var cumulative = 0;

        (string eggName, string emoji, int rarity, long reward) found = _easterEggs[0];
        foreach (var egg in _easterEggs)
        {
            cumulative += egg.rarity;
            if (roll < cumulative)
            {
                found = egg;
                break;
            }
        }

        // Record the claim
        ctx.Add(new SeasonalClaim
        {
            UserId = userId,
            GuildId = guildId,
            ClaimType = "egghunt",
            Day = 0,
            ClaimedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        // Award seasonal currency
        await _cs.AddAsync(userId, found.reward,
            new("seasonal", "egghunt", $"Found {found.eggName}"));

        return (true,
            $"{found.emoji} You found a **{found.eggName}**!\n"
            + $"Reward: **{found.reward}** \U0001f36b Chocolate");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static bool IsDateInRange(DateTime now, SeasonalEvent ev)
    {
        // Handle events that wrap around year boundary (e.g. New Year: Dec 31 - Jan 2)
        if (ev.EndMonth < ev.StartMonth)
        {
            // Either we're in the start-year portion (>= start) or new-year portion (<= end)
            var afterStart = now.Month > ev.StartMonth
                || (now.Month == ev.StartMonth && now.Day >= ev.StartDay);
            var beforeEnd = now.Month < ev.EndMonth
                || (now.Month == ev.EndMonth && now.Day <= ev.EndDay);
            return afterStart || beforeEnd;
        }

        var onOrAfterStart = now.Month > ev.StartMonth
            || (now.Month == ev.StartMonth && now.Day >= ev.StartDay);
        var onOrBeforeEnd = now.Month < ev.EndMonth
            || (now.Month == ev.EndMonth && now.Day <= ev.EndDay);
        return onOrAfterStart && onOrBeforeEnd;
    }
}
