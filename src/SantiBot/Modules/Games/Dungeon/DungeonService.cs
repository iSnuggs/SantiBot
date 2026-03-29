#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;
using System.Text;

namespace SantiBot.Modules.Games.Dungeon;

public sealed class DungeonService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, DungeonRun> ActiveDungeons = new();

    // Raid boss random spawn callback — set by RaidBossService to avoid circular DI
    public Func<ulong, Task<(bool Spawned, string Message, ulong ChannelId)>> OnDungeonClearedAsync { get; set; }

    public DungeonService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    // ═══════════════════════════════════════════════════════════
    //  XP / LEVELING
    // ═══════════════════════════════════════════════════════════

    public static long XpForLevel(int level) => (long)(100 * Math.Pow(1.5, level - 1));

    public static int CalculateLevel(long totalXp)
    {
        var level = 1;
        var needed = 100L;
        while (totalXp >= needed)
        {
            totalXp -= needed;
            level++;
            needed = (long)(100 * Math.Pow(1.5, level - 1));
        }
        return level;
    }

    // ═══════════════════════════════════════════════════════════
    //  11 CLASSES
    // ═══════════════════════════════════════════════════════════

    public static readonly Dictionary<string, (int HpMult, int AtkMult, int DefMult, string Desc, string Emoji)> Classes = new()
    {
        ["Warrior"]     = (130, 110, 120, "Tank — Second Wind heal + Cleave on kills + Heavy Armor", "🛡️"),
        ["Mage"]        = (80,  150, 70,  "Nuker — Fireball AoE + Arcane Ward shield", "🔮"),
        ["Rogue"]       = (90,  130, 90,  "Striker — Sneak Attack crits + poison + trap disarm", "🗡️"),
        ["Cleric"]      = (110, 90,  110, "Support — party heals + Turn Undead + Divine Shield", "✝️"),
        ["Monk"]        = (95,  125, 105, "Martial artist — Flurry of Blows + Evasion dodge", "👊"),
        ["Barbarian"]   = (120, 140, 60,  "Berserker — Rage mode + Reckless Attack + Relentless", "⚔️"),
        ["Ranger"]      = (100, 120, 100, "Scout — First Strike + Tracking + Nature's Ally", "🏹"),
        ["Paladin"]     = (120, 105, 115, "Holy knight — Divine Smite + Lay on Hands + Aura of Protection", "⚜️"),
        ["Bard"]        = (90,  100, 85,  "Buffer — Inspiration + Song of Rest + Vicious Mockery", "🎵"),
        ["Necromancer"] = (85,  140, 75,  "Summoner — Raise Skeleton + Life Drain + Death Ward", "💀"),
        ["Druid"]       = (105, 110, 100, "Shapeshifter — Wild Shape + Entangle + Rejuvenation", "🌿"),
    };

    // ═══════════════════════════════════════════════════════════
    //  8 RACES
    // ═══════════════════════════════════════════════════════════

    public static readonly Dictionary<string, (int HpMod, int AtkMod, int DefMod, string Desc, string Emoji, string Ability)> Races = new()
    {
        ["Human"]      = (105, 105, 105, "Balanced — +5% all stats, +15% XP gain",            "🧑", "Versatile"),
        ["Elf"]        = (100, 110, 105, "Agile — +10% ATK, +5% DEF, immune to sleep traps",  "🧝", "Fey Ancestry"),
        ["Dwarf"]      = (115, 100, 110, "Sturdy — +15% HP, +10% DEF, 30% less trap damage",  "⛏️", "Stone Skin"),
        ["Halfling"]   = (100, 105, 100, "Lucky — +5% ATK, reroll one dodge/flee per dungeon", "🍀", "Lucky"),
        ["Orc"]        = (105, 120, 90,  "Savage — +20% ATK, -10% DEF, crits deal 2.5x",      "💪", "Savage Crits"),
        ["Dragonborn"] = (110, 110, 100, "Draconic — +10% HP/ATK, 15% Breath Weapon on atk",  "🐲", "Breath Weapon"),
        ["Tiefling"]   = (95,  115, 100, "Infernal — +15% ATK, -5% HP, bonus dmg below 50%",  "😈", "Infernal Wrath"),
        ["Gnome"]      = (105, 100, 105, "Clever — +5% DEF/HP, +20% trap disarm chance",      "🔧", "Tinker"),
    };

    // ═══════════════════════════════════════════════════════════
    //  STAT CALCULATION (Class + Race + Equipment)
    // ═══════════════════════════════════════════════════════════

    public static (int MaxHp, int Attack, int Defense) GetEffectiveStats(DungeonPlayer player, List<DungeonItem> equipped)
    {
        var hp  = player.BaseHp  + (player.Level - 1) * 15;
        var atk = player.BaseAttack + (player.Level - 1) * 4;
        var def = player.BaseDefense + (player.Level - 1) * 3;

        // Class multipliers
        if (Classes.TryGetValue(player.Class, out var cls))
        {
            hp  = hp * cls.HpMult / 100;
            atk = atk * cls.AtkMult / 100;
            def = def * cls.DefMult / 100;
        }

        // Race multipliers (stacks with class)
        if (Races.TryGetValue(player.Race, out var race))
        {
            hp  = hp * race.HpMod / 100;
            atk = atk * race.AtkMod / 100;
            def = def * race.DefMod / 100;
        }

        // Equipment bonuses
        foreach (var item in equipped)
        {
            hp  += item.BonusHp;
            atk += item.BonusAttack;
            def += item.BonusDefense;
        }

        return (Math.Max(1, hp), Math.Max(1, atk), Math.Max(0, def));
    }

    // ═══════════════════════════════════════════════════════════
    //  LOOT TABLES
    // ═══════════════════════════════════════════════════════════

    private static readonly (string Name, string Slot, int Atk, int Def, int Hp, string Special)[] WeaponDrops =
    [
        ("Rusty Sword",       "Weapon", 5,  0, 0,  null),
        ("Iron Axe",          "Weapon", 10, 0, 0,  null),
        ("Flame Dagger",      "Weapon", 15, 0, 0,  "Burn 5% HP/turn"),
        ("Shadow Blade",      "Weapon", 20, 0, 0,  "Crit +10%"),
        ("Arcane Staff",      "Weapon", 25, 0, 10, "Spell Amp +15%"),
        ("Vorpal Greatsword", "Weapon", 35, 0, 0,  "Lifesteal 10%"),
        ("Dragon Fang",       "Weapon", 50, 5, 20, "Double strike 8%"),
    ];

    private static readonly (string Name, string Slot, int Atk, int Def, int Hp, string Special)[] ArmorDrops =
    [
        ("Leather Vest",      "Armor", 0, 5,  10,  null),
        ("Chainmail",         "Armor", 0, 10, 20,  null),
        ("Plated Armor",      "Armor", 0, 18, 40,  null),
        ("Mage Robes",        "Armor", 8, 5,  15,  "Mana Regen"),
        ("Shadow Cloak",      "Armor", 5, 12, 25,  "Dodge +10%"),
        ("Dragon Scale Mail", "Armor", 5, 30, 60,  "Fire Resist"),
        ("Mythril Plate",     "Armor", 10,40, 80,  "All Resist +15%"),
    ];

    private static readonly (string Name, string Slot, int Atk, int Def, int Hp, string Special)[] AccessoryDrops =
    [
        ("Lucky Coin",        "Accessory", 0, 0,  0,  "Loot +10%"),
        ("Ring of Vigor",     "Accessory", 0, 0,  30, "Regen 5 HP/room"),
        ("Amulet of Power",   "Accessory", 12,0,  0,  null),
        ("Ward Stone",        "Accessory", 0, 15, 20, null),
        ("Berserker Band",    "Accessory", 20,0,  0,  "Crit +15%"),
        ("Phoenix Feather",   "Accessory", 0, 0,  50, "Revive once"),
        ("Crown of Kings",    "Accessory", 15,15, 50, "XP +25%"),
    ];

    private static readonly string[] Rarities = ["Common", "Common", "Common", "Uncommon", "Uncommon", "Rare", "Epic"];

    private DungeonItem GenerateLootDrop(int difficulty, ulong userId, ulong guildId)
    {
        var maxTier = Math.Min(WeaponDrops.Length - 1, difficulty + 1);
        var tier = _rng.Next(0, maxTier + 1);
        var slotRoll = _rng.Next(3);
        var (name, slot, atk, def, hp, special) = slotRoll switch
        {
            0 => WeaponDrops[tier],
            1 => ArmorDrops[tier],
            _ => AccessoryDrops[tier],
        };

        var rarityIdx = Math.Min(tier, Rarities.Length - 1);
        var rarity = Rarities[rarityIdx];
        if (difficulty >= 4 && _rng.Next(100) < 5)
            rarity = "Legendary";

        var mult = rarity switch
        {
            "Uncommon"  => 1.3,
            "Rare"      => 1.6,
            "Epic"      => 2.0,
            "Legendary" => 3.0,
            _           => 1.0,
        };

        return new DungeonItem
        {
            UserId = userId,
            GuildId = guildId,
            Name = rarity == "Legendary" ? $"Legendary {name}" : name,
            Slot = slot,
            Rarity = rarity,
            BonusAttack = (int)(atk * mult),
            BonusDefense = (int)(def * mult),
            BonusHp = (int)(hp * mult),
            SpecialEffect = special,
        };
    }

    public static string RarityEmoji(string rarity) => rarity switch
    {
        "Common" => "⬜", "Uncommon" => "🟩", "Rare" => "🟦",
        "Epic" => "🟪", "Legendary" => "🟧", _ => "⬜",
    };

    // ═══════════════════════════════════════════════════════════
    //  DB HELPERS
    // ═══════════════════════════════════════════════════════════

    public async Task<DungeonPlayer> GetOrCreatePlayerAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (player is not null)
            return player;

        player = new DungeonPlayer { UserId = userId, GuildId = guildId };
        ctx.Add(player);
        await ctx.SaveChangesAsync();
        return player;
    }

    public async Task SavePlayerAsync(DungeonPlayer player)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<DungeonPlayer>()
            .Where(x => x.Id == player.Id)
            .UpdateAsync(_ => new DungeonPlayer
            {
                Level = player.Level,
                Xp = player.Xp,
                BaseHp = player.BaseHp,
                BaseAttack = player.BaseAttack,
                BaseDefense = player.BaseDefense,
                Class = player.Class,
                Race = player.Race,
                DungeonsCleared = player.DungeonsCleared,
                MonstersKilled = player.MonstersKilled,
                TotalLoot = player.TotalLoot,
                HighestDifficulty = player.HighestDifficulty,
                DeathCount = player.DeathCount,
                WeaponId = player.WeaponId,
                ArmorId = player.ArmorId,
                AccessoryId = player.AccessoryId,
            });
    }

    public async Task<List<DungeonItem>> GetEquippedItemsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<DungeonItem>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.IsEquipped)
            .ToListAsyncLinqToDB();
    }

    public async Task<List<DungeonItem>> GetInventoryAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<DungeonItem>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .OrderByDescending(x => x.Rarity)
            .ThenBy(x => x.Slot)
            .ToListAsyncLinqToDB();
    }

    public async Task<DungeonItem> SaveItemAsync(DungeonItem item)
    {
        await using var ctx = _db.GetDbContext();
        ctx.Add(item);
        await ctx.SaveChangesAsync();
        return item;
    }

    public async Task<bool> EquipItemAsync(ulong userId, ulong guildId, int itemId)
    {
        await using var ctx = _db.GetDbContext();
        var item = await ctx.GetTable<DungeonItem>()
            .FirstOrDefaultAsyncLinqToDB(x => x.Id == itemId && x.UserId == userId && x.GuildId == guildId);

        if (item is null || item.Slot == "Consumable")
            return false;

        await ctx.GetTable<DungeonItem>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.Slot == item.Slot && x.IsEquipped)
            .UpdateAsync(_ => new DungeonItem { IsEquipped = false });

        await ctx.GetTable<DungeonItem>()
            .Where(x => x.Id == itemId)
            .UpdateAsync(_ => new DungeonItem { IsEquipped = true });

        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (player is not null)
        {
            switch (item.Slot)
            {
                case "Weapon":
                    await ctx.GetTable<DungeonPlayer>().Where(x => x.Id == player.Id)
                        .UpdateAsync(_ => new DungeonPlayer { WeaponId = itemId });
                    break;
                case "Armor":
                    await ctx.GetTable<DungeonPlayer>().Where(x => x.Id == player.Id)
                        .UpdateAsync(_ => new DungeonPlayer { ArmorId = itemId });
                    break;
                case "Accessory":
                    await ctx.GetTable<DungeonPlayer>().Where(x => x.Id == player.Id)
                        .UpdateAsync(_ => new DungeonPlayer { AccessoryId = itemId });
                    break;
            }
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    //  RUN MODEL + PARTY MEMBER
    // ═══════════════════════════════════════════════════════════

    public class DungeonRun
    {
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public List<PartyMember> Party { get; set; } = new();
        public int Difficulty { get; set; }
        public int CurrentRoom { get; set; }
        public int MaxRooms { get; set; }
        public long TotalLoot { get; set; }
        public string CurrentMonster { get; set; }
        public int MonsterHp { get; set; }
        public int MonsterAtk { get; set; }
        public int MonsterDef { get; set; }
        public long XpPool { get; set; }
        public List<DungeonItem> LootDrops { get; set; } = new();

        // Bard tracking
        public ulong BardInspireTarget { get; set; } // next attack boosted
        public bool BardMockeryActive { get; set; }   // monster ATK debuffed

        // Necromancer tracking
        public int SkeletonHp { get; set; }       // raised skeleton HP (0 = no skeleton)
        public int SkeletonAtk { get; set; }

        // Druid tracking
        public Dictionary<ulong, int> WildShapeHp { get; set; } = new(); // bonus HP pool per player

        // Barbarian tracking
        public HashSet<ulong> RagingPlayers { get; set; } = new();

        // Halfling Lucky — one reroll per dungeon per halfling
        public HashSet<ulong> LuckyUsed { get; set; } = new();

        // Ranger tracking
        public string NextRoomPreview { get; set; } // from Tracking ability
    }

    public class PartyMember
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Class { get; set; }
        public string Race { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Level { get; set; }
        public int MonstersKilled { get; set; }
        public bool HasRevive { get; set; }
        public bool DeathWardActive { get; set; } // Necromancer
    }

    // ═══════════════════════════════════════════════════════════
    //  MONSTERS
    // ═══════════════════════════════════════════════════════════

    private static readonly (string Name, string Emoji, int BaseHp, int BaseAtk, int BaseDef, long BaseXp)[] Monsters =
    [
        ("Goblin",         "👺", 30,  8,   3,  15),
        ("Skeleton",       "💀", 40,  12,  5,  20),
        ("Dark Mage",      "🧙", 35,  18,  4,  25),
        ("Orc Warrior",    "👹", 60,  15,  8,  30),
        ("Giant Spider",   "🕷️", 45,  14,  6,  25),
        ("Undead Knight",  "⚔️", 70,  20,  12, 40),
        ("Fire Drake",     "🐉", 80,  25,  15, 55),
        ("Shadow Demon",   "👿", 100, 30,  18, 70),
        ("Lich King",      "☠️", 120, 35,  22, 90),
        ("Ancient Dragon", "🐲", 150, 40,  28, 120),
    ];

    // ═══════════════════════════════════════════════════════════
    //  ENTER DUNGEON
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> EnterDungeonAsync(
        ulong channelId, ulong guildId, ulong userId, string username, int difficulty)
    {
        if (ActiveDungeons.ContainsKey(channelId))
            return (false, "A dungeon run is already in progress!");

        difficulty = Math.Clamp(difficulty, 1, 5);

        var player = await GetOrCreatePlayerAsync(userId, guildId);
        var equipped = await GetEquippedItemsAsync(userId, guildId);
        var (maxHp, atk, def) = GetEffectiveStats(player, equipped);
        var hasRevive = equipped.Any(e => e.SpecialEffect == "Revive once");

        var run = new DungeonRun
        {
            ChannelId = channelId,
            GuildId = guildId,
            Difficulty = difficulty,
            CurrentRoom = 0,
            MaxRooms = 5 + difficulty * 2,
            Party =
            [
                new PartyMember
                {
                    UserId = userId, Username = username,
                    Class = player.Class, Race = player.Race,
                    Hp = maxHp, MaxHp = maxHp, Attack = atk, Defense = def,
                    Level = player.Level, HasRevive = hasRevive,
                    DeathWardActive = player.Class == "Necromancer",
                }
            ]
        };

        // Barbarian starts in Rage
        if (player.Class == "Barbarian")
            run.RagingPlayers.Add(userId);

        ActiveDungeons[channelId] = run;

        var classEmoji = Classes.TryGetValue(player.Class, out var c) ? c.Emoji : "⚔️";
        var raceEmoji = Races.TryGetValue(player.Race, out var r) ? r.Emoji : "🧑";
        return (true, $"⚔️ **Dungeon Run** (Difficulty {difficulty})\n" +
            $"{raceEmoji}{classEmoji} **{username}** enters! (Lv.{player.Level} {player.Race} {player.Class})\n" +
            $"HP: {maxHp} | ATK: {atk} | DEF: {def}\n" +
            $"Rooms: {run.MaxRooms}\n" +
            $"Invite with `.dungeon invite @user` or `.dungeon explore` to begin!");
    }

    // ═══════════════════════════════════════════════════════════
    //  INVITE
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> InviteToPartyAsync(
        ulong channelId, ulong guildId, ulong userId, string username)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");
        if (run.Party.Count >= 4)
            return (false, "Party is full (max 4)!");
        if (run.Party.Any(p => p.UserId == userId))
            return (false, "Already in the party!");

        var player = await GetOrCreatePlayerAsync(userId, guildId);
        var equipped = await GetEquippedItemsAsync(userId, guildId);
        var (maxHp, atk, def) = GetEffectiveStats(player, equipped);
        var hasRevive = equipped.Any(e => e.SpecialEffect == "Revive once");

        run.Party.Add(new PartyMember
        {
            UserId = userId, Username = username,
            Class = player.Class, Race = player.Race,
            Hp = maxHp, MaxHp = maxHp, Attack = atk, Defense = def,
            Level = player.Level, HasRevive = hasRevive,
            DeathWardActive = player.Class == "Necromancer",
        });

        if (player.Class == "Barbarian")
            run.RagingPlayers.Add(userId);

        var classEmoji = Classes.TryGetValue(player.Class, out var c) ? c.Emoji : "⚔️";
        var raceEmoji = Races.TryGetValue(player.Race, out var r) ? r.Emoji : "🧑";
        return (true, $"{raceEmoji}{classEmoji} **{username}** (Lv.{player.Level} {player.Race} {player.Class}) joins! ({run.Party.Count}/4)");
    }

    // ═══════════════════════════════════════════════════════════
    //  EXPLORE (room events)
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> ExploreAsync(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");
        if (run.CurrentMonster is not null)
            return (false, "Defeat the monster first! Use `.dungeon attack` or `.dungeon flee`.");

        run.CurrentRoom++;
        var sb = new StringBuilder();
        sb.AppendLine($"🚪 **Room {run.CurrentRoom}/{run.MaxRooms}**\n");

        // ── Ranger: show preview from last Tracking ──
        if (run.NextRoomPreview is not null)
        {
            sb.AppendLine($"🏹 *Ranger's scouting report: {run.NextRoomPreview}*\n");
            run.NextRoomPreview = null;
        }

        // ── Cleric: Healing Aura each room ──
        foreach (var p in run.Party.Where(p => p.Class == "Cleric"))
        {
            var heal = 10 + p.Level * 2;
            foreach (var ally in run.Party)
            {
                var oldHp = ally.Hp;
                ally.Hp = Math.Min(ally.MaxHp, ally.Hp + heal);
                if (ally.Hp > oldHp)
                    sb.AppendLine($"✝️ {p.Username}'s healing aura restores {ally.Hp - oldHp} HP to {ally.Username}!");
            }
        }

        // ── Druid: Rejuvenation — regen HP each room ──
        foreach (var p in run.Party.Where(p => p.Class == "Druid"))
        {
            var regen = 8 + p.Level;
            var oldHp = p.Hp;
            p.Hp = Math.Min(p.MaxHp, p.Hp + regen);
            if (p.Hp > oldHp)
                sb.AppendLine($"🌿 {p.Username}'s Rejuvenation restores {p.Hp - oldHp} HP!");
        }

        // ── Bard: Song of Rest — heal party between rooms ──
        foreach (var bard in run.Party.Where(p => p.Class == "Bard"))
        {
            var songHeal = 5 + bard.Level;
            foreach (var ally in run.Party)
            {
                var oldHp = ally.Hp;
                ally.Hp = Math.Min(ally.MaxHp, ally.Hp + songHeal);
                if (ally.Hp > oldHp)
                    sb.AppendLine($"🎵 {bard.Username}'s Song of Rest heals {ally.Hp - oldHp} HP for {ally.Username}!");
            }
        }

        // ── Paladin: Aura of Protection — passive DEF buff ──
        // (applied during stat calc, but announce it)
        if (run.Party.Any(p => p.Class == "Paladin") && run.CurrentRoom == 1)
            sb.AppendLine($"⚜️ Paladin's **Aura of Protection** grants +10% DEF to all allies!");

        var eventType = _rng.Next(10);

        if (eventType < 6) // 60% monster
        {
            var monsterIdx = Math.Min(_rng.Next(Monsters.Length), run.Difficulty * 2);
            var (name, emoji, baseHp, baseAtk, baseDef, baseXp) = Monsters[monsterIdx];
            run.CurrentMonster = $"{emoji} {name}";
            run.MonsterHp = baseHp * run.Difficulty;
            run.MonsterAtk = baseAtk * run.Difficulty;
            run.MonsterDef = baseDef * run.Difficulty;

            // Bard Vicious Mockery carries over
            if (run.BardMockeryActive)
            {
                run.MonsterAtk = run.MonsterAtk * 80 / 100;
                sb.AppendLine($"🎵 The Bard's mockery lingers — monster ATK reduced!");
                run.BardMockeryActive = false;
            }

            // ── Ranger: First Strike — bonus damage on first encounter ──
            var ranger = run.Party.FirstOrDefault(p => p.Class == "Ranger");
            if (ranger is not null)
            {
                var firstStrike = ranger.Attack / 2;
                run.MonsterHp -= firstStrike;
                sb.AppendLine($"🏹 **First Strike!** {ranger.Username} fires an arrow for {firstStrike} before combat!");
            }

            sb.AppendLine($"{emoji} A **{name}** appears! (HP: {run.MonsterHp}, ATK: {run.MonsterAtk}, DEF: {run.MonsterDef})");
            sb.AppendLine("Use `.dungeon attack` to fight or `.dungeon flee` to run!");

            // ── Ranger: Tracking — preview next room ──
            if (ranger is not null && run.CurrentRoom < run.MaxRooms)
            {
                var nextEvent = _rng.Next(10);
                run.NextRoomPreview = nextEvent < 6 ? "Monster ahead" : nextEvent < 8 ? "Treasure room" : nextEvent < 9 ? "Trap detected" : "Safe room";
            }

            // ── Ranger: Nature's Ally — 10% to tame weak monsters ──
            if (ranger is not null && monsterIdx <= 2 && _rng.Next(100) < 10)
            {
                run.CurrentMonster = null;
                var tameXp = baseXp * run.Difficulty;
                run.XpPool += tameXp;
                sb.Clear();
                sb.AppendLine($"🚪 **Room {run.CurrentRoom}/{run.MaxRooms}**\n");
                sb.AppendLine($"🏹 **Nature's Ally!** {ranger.Username} tames the {name}! +{tameXp} XP");
                sb.AppendLine("The creature guides you safely. Use `.dungeon explore` to continue!");
            }
        }
        else if (eventType < 8) // 20% treasure
        {
            var loot = _rng.Next(50, 200) * run.Difficulty;
            run.TotalLoot += loot;
            sb.AppendLine($"💰 **Treasure found!** +{loot} 🥠 (Total: {run.TotalLoot})");

            if (_rng.Next(100) < 30 + run.Difficulty * 10)
            {
                var owner = run.Party[_rng.Next(run.Party.Count)];
                var drop = GenerateLootDrop(run.Difficulty, owner.UserId, run.GuildId);
                run.LootDrops.Add(drop);
                sb.AppendLine($"{RarityEmoji(drop.Rarity)} **{owner.Username}** found: **{drop.Name}** ({drop.Rarity} {drop.Slot})");
                if (drop.BonusAttack > 0) sb.Append($" ATK+{drop.BonusAttack}");
                if (drop.BonusDefense > 0) sb.Append($" DEF+{drop.BonusDefense}");
                if (drop.BonusHp > 0) sb.Append($" HP+{drop.BonusHp}");
                if (drop.SpecialEffect is not null) sb.Append($" *{drop.SpecialEffect}*");
                sb.AppendLine();
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);
            sb.AppendLine("Continue with `.dungeon explore`!");
        }
        else if (eventType < 9) // 10% trap
        {
            var damage = _rng.Next(10, 30) * run.Difficulty;

            // Rogue + Gnome trap disarm
            var rogue = run.Party.FirstOrDefault(p => p.Class == "Rogue");
            var disarmChance = 0;
            if (rogue is not null) disarmChance += 40;
            // Gnome racial adds +20% disarm even without Rogue
            var gnome = run.Party.FirstOrDefault(p => p.Race == "Gnome");
            if (gnome is not null) disarmChance += 20;

            if (disarmChance > 0 && _rng.Next(100) < disarmChance)
            {
                var disarmer = rogue ?? gnome;
                sb.AppendLine($"🔧 **Trap Disarmed!** {disarmer.Username} spots the mechanism and disables it!");
                run.XpPool += 10L * run.Difficulty;
                sb.AppendLine($"  +{10L * run.Difficulty} XP for the party!");
            }
            else
            {
                sb.AppendLine($"💥 **Trap!**");
                foreach (var p in run.Party)
                {
                    var reduced = Math.Max(1, damage - p.Defense / 2);

                    // Elf: Fey Ancestry — immune to sleep traps (50% less all trap dmg)
                    if (p.Race == "Elf") reduced = Math.Max(1, reduced * 50 / 100);
                    // Dwarf: Stone Skin — 30% less trap damage
                    if (p.Race == "Dwarf") reduced = Math.Max(1, reduced * 70 / 100);
                    // Monk: Evasion — half trap damage
                    if (p.Class == "Monk") reduced = Math.Max(1, reduced / 2);
                    // Warrior: Heavy Armor — 25% less
                    if (p.Class == "Warrior") reduced = Math.Max(1, reduced * 75 / 100);
                    // Mage: Arcane Ward chance on traps
                    if (p.Class == "Mage" && _rng.Next(100) < 25)
                    {
                        var absorbed = Math.Min(reduced, 10 + p.Level * 2);
                        reduced = Math.Max(0, reduced - absorbed);
                        sb.AppendLine($"  🔮 {p.Username}'s Arcane Ward absorbs {absorbed}!");
                    }
                    // Druid: Wild Shape absorbs first
                    if (p.Class == "Druid" && run.WildShapeHp.TryGetValue(p.UserId, out var wsHp) && wsHp > 0)
                    {
                        var absorbed = Math.Min(reduced, wsHp);
                        run.WildShapeHp[p.UserId] = wsHp - absorbed;
                        reduced -= absorbed;
                        sb.AppendLine($"  🌿 {p.Username}'s Wild Shape absorbs {absorbed}!");
                    }

                    p.Hp = Math.Max(0, p.Hp - reduced);
                    var tag = "";
                    if (p.Race == "Elf") tag = " *(Fey Ancestry)*";
                    else if (p.Race == "Dwarf") tag = " *(Stone Skin)*";
                    else if (p.Class == "Monk") tag = " *(Evasion)*";
                    else if (p.Class == "Warrior") tag = " *(Heavy Armor)*";
                    sb.AppendLine($"  {p.Username} takes {reduced} damage ({p.Hp}/{p.MaxHp} HP){tag}");
                }
            }

            HandleDeaths(run, sb);
            if (run.Party.Count == 0)
            {
                ActiveDungeons.TryRemove(channelId, out _);
                sb.AppendLine("💀 **Party wiped!** Dungeon failed.");
                return (true, sb.ToString());
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);
            sb.AppendLine("Continue with `.dungeon explore`!");
        }
        else // 10% healing spring
        {
            var heal = 30 * run.Difficulty;
            sb.AppendLine($"✨ **Healing Spring!** Party restores {heal} HP!");
            foreach (var p in run.Party)
                p.Hp = Math.Min(p.MaxHp, p.Hp + heal);

            // Druid: Wild Shape recharges at springs
            foreach (var druid in run.Party.Where(p => p.Class == "Druid"))
            {
                var wsBonus = 20 + druid.Level * 5;
                run.WildShapeHp[druid.UserId] = wsBonus;
                sb.AppendLine($"🌿 {druid.Username}'s **Wild Shape** recharges! (+{wsBonus} bonus HP shield)");
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);
            sb.AppendLine("Continue with `.dungeon explore`!");
        }

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  ATTACK
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> AttackAsync(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");
        if (run.CurrentMonster is null)
            return (false, "No monster to fight! Use `.dungeon explore`.");

        var sb = new StringBuilder();

        // ── Necromancer: Raised Skeleton attacks first ──
        if (run.SkeletonHp > 0)
        {
            var skelDmg = Math.Max(1, run.SkeletonAtk - run.MonsterDef / 4);
            run.MonsterHp -= skelDmg;
            sb.AppendLine($"💀 Raised Skeleton attacks for {skelDmg}!");
        }

        // ── Each party member attacks ──
        foreach (var p in run.Party)
        {
            var baseDmg = Math.Max(1, p.Attack - run.MonsterDef / 3);
            var damage = baseDmg + _rng.Next(-3, 8);

            // Paladin Aura — +10% DEF for whole party (applied to defense checks below)

            // Bard Inspiration — boosted target gets +50% damage
            if (run.BardInspireTarget == p.UserId)
            {
                damage = (int)(damage * 1.5);
                sb.AppendLine($"🎵 **Inspired!** {p.Username} strikes with vigor!");
                run.BardInspireTarget = 0;
            }

            // ── Tiefling: Infernal Wrath — +30% damage when below 50% HP ──
            if (p.Race == "Tiefling" && p.Hp <= p.MaxHp / 2)
                damage = (int)(damage * 1.3);

            // ═══ CLASS ATTACK ABILITIES ═══

            // Rogue: Sneak Attack (25%, 2x + poison)
            if (p.Class == "Rogue" && _rng.Next(100) < 25)
            {
                damage = (int)(damage * 2.0);
                var poison = Math.Max(1, baseDmg / 4);
                run.MonsterHp -= poison;
                sb.AppendLine($"🗡️ **Sneak Attack!** {p.Username} deals {damage} + {poison} poison!");
            }
            // Mage: Fireball (20%, 1.8x)
            else if (p.Class == "Mage" && _rng.Next(100) < 20)
            {
                damage = (int)(damage * 1.8);
                sb.AppendLine($"🔮 **Fireball!** {p.Username} blasts for {damage}!");
            }
            // Warrior: Power Attack (always +25%)
            else if (p.Class == "Warrior")
            {
                damage = (int)(damage * 1.25);
                sb.AppendLine($"🛡️ {p.Username} lands a heavy blow for {damage}!");
            }
            // Cleric: Turn Undead (2x vs undead)
            else if (p.Class == "Cleric" && run.CurrentMonster is not null &&
                (run.CurrentMonster.Contains("Skeleton") || run.CurrentMonster.Contains("Undead") || run.CurrentMonster.Contains("Lich")))
            {
                damage = (int)(damage * 2.0);
                sb.AppendLine($"✝️ **Turn Undead!** {p.Username} smites for {damage}!");
            }
            // Barbarian: Reckless Attack (35% chance for 2x, but takes 15% recoil)
            else if (p.Class == "Barbarian" && _rng.Next(100) < 35)
            {
                damage = (int)(damage * 2.0);
                var recoil = Math.Max(1, damage * 15 / 100);
                p.Hp = Math.Max(1, p.Hp - recoil);
                sb.AppendLine($"⚔️ **Reckless Attack!** {p.Username} deals {damage} (takes {recoil} recoil, {p.Hp} HP)!");
            }
            // Paladin: Divine Smite (20% chance for 1.7x + extra vs undead)
            else if (p.Class == "Paladin" && _rng.Next(100) < 20)
            {
                var smiteMult = 1.7;
                if (run.CurrentMonster is not null &&
                    (run.CurrentMonster.Contains("Skeleton") || run.CurrentMonster.Contains("Undead") || run.CurrentMonster.Contains("Lich")))
                    smiteMult = 2.5;
                damage = (int)(damage * smiteMult);
                sb.AppendLine($"⚜️ **Divine Smite!** {p.Username} channels holy light for {damage}!");
            }
            // Bard: Vicious Mockery (debuffs monster ATK by 20% for next round)
            else if (p.Class == "Bard")
            {
                sb.AppendLine($"🎵 {p.Username} deals {damage} and hurls a **Vicious Mockery**!");
                run.BardMockeryActive = true;
            }
            // Necromancer: Life Drain (heal 30% of damage dealt)
            else if (p.Class == "Necromancer")
            {
                damage = (int)(damage * 1.1);
                var drain = Math.Max(1, damage * 30 / 100);
                p.Hp = Math.Min(p.MaxHp, p.Hp + drain);
                sb.AppendLine($"💀 **Life Drain!** {p.Username} deals {damage} and heals {drain} HP!");
            }
            // Druid: Entangle (15% chance to skip monster's counterattack)
            else if (p.Class == "Druid" && _rng.Next(100) < 15)
            {
                sb.AppendLine($"🌿 **Entangle!** {p.Username} deals {damage} — vines hold the monster!");
                run.MonsterHp -= damage;
                // Skip counterattack this round only — save and restore after counterattack section
                run.MonsterAtk = -1; // sentinel value: skip counterattack this round
                continue; // skip normal damage apply since we already did it
            }
            else
            {
                sb.AppendLine($"⚔️ {p.Username} deals {damage}!");
            }

            // ═══ RACIAL ATTACK ABILITIES ═══

            // Orc: Savage Crits — crits deal 2.5x instead of 2x (already factored in Rogue/Barb, add extra .5)
            if (p.Race == "Orc" && damage > baseDmg * 1.5) // was a crit-like hit
            {
                var orcBonus = Math.Max(1, baseDmg / 4);
                damage += orcBonus;
                run.MonsterHp -= orcBonus;
                sb.AppendLine($"💪 **Savage Crits!** Orc fury adds {orcBonus} bonus damage!");
            }

            // Dragonborn: Breath Weapon — 15% chance for bonus fire AoE
            if (p.Race == "Dragonborn" && _rng.Next(100) < 15)
            {
                var breathDmg = 10 + p.Level * 3;
                run.MonsterHp -= breathDmg;
                sb.AppendLine($"🐲 **Breath Weapon!** {p.Username} breathes fire for {breathDmg}!");
            }

            run.MonsterHp -= damage;

            // ═══ POST-HIT CLASS ABILITIES ═══

            // Monk: Flurry of Blows (30% bonus strike)
            if (p.Class == "Monk" && _rng.Next(100) < 30)
            {
                var bonusHit = Math.Max(1, baseDmg / 2 + _rng.Next(0, 6));
                run.MonsterHp -= bonusHit;
                sb.AppendLine($"👊 **Flurry of Blows!** {p.Username} strikes again for {bonusHit}!");
            }

            // Warrior: Cleave bonus on kill
            if (p.Class == "Warrior" && run.MonsterHp <= 0 && run.CurrentRoom < run.MaxRooms)
            {
                sb.AppendLine($"🛡️ **Cleave!** {p.Username}'s killing blow echoes!");
                run.TotalLoot += 20 * run.Difficulty;
            }

            // Bard: auto-inspire random ally for next round
            if (p.Class == "Bard")
            {
                var allies = run.Party.Where(a => a.UserId != p.UserId).ToList();
                if (allies.Count > 0)
                {
                    var target = allies[_rng.Next(allies.Count)];
                    run.BardInspireTarget = target.UserId;
                    sb.AppendLine($"🎵 {p.Username} inspires **{target.Username}** for their next attack!");
                }
            }
        }

        // ── Monster defeated? ──
        if (run.MonsterHp <= 0)
        {
            var monsterName = run.CurrentMonster;
            var monsterIdx = Array.FindIndex(Monsters, m => monsterName?.Contains(m.Name) == true);
            var xpReward = monsterIdx >= 0 ? Monsters[monsterIdx].BaseXp * run.Difficulty : 20L * run.Difficulty;

            // Human: Versatile — +15% XP
            if (run.Party.Any(p => p.Race == "Human"))
                xpReward = xpReward * 115 / 100;

            var loot = _rng.Next(30, 100) * run.Difficulty;
            run.TotalLoot += loot;
            run.XpPool += xpReward;

            foreach (var p in run.Party)
                p.MonstersKilled++;

            // ── Necromancer: Raise Skeleton from killed monster ──
            var necro = run.Party.FirstOrDefault(p => p.Class == "Necromancer");
            if (necro is not null && run.SkeletonHp <= 0 && _rng.Next(100) < 40)
            {
                run.SkeletonHp = 30 + necro.Level * 5;
                run.SkeletonAtk = 10 + necro.Level * 2;
                sb.AppendLine($"💀 **Raise Skeleton!** {necro.Username} reanimates the corpse! (HP: {run.SkeletonHp}, ATK: {run.SkeletonAtk})");
            }

            run.CurrentMonster = null;
            sb.AppendLine($"\n💀 **Monster defeated!** +{loot} 🥠 | +{xpReward} XP");

            if (_rng.Next(100) < 20 + run.Difficulty * 5)
            {
                var owner = run.Party[_rng.Next(run.Party.Count)];
                var drop = GenerateLootDrop(run.Difficulty, owner.UserId, run.GuildId);
                run.LootDrops.Add(drop);
                sb.AppendLine($"{RarityEmoji(drop.Rarity)} **{owner.Username}** looted: **{drop.Name}** ({drop.Rarity} {drop.Slot})");
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);

            sb.AppendLine("Use `.dungeon explore` for the next room!");
            return (true, sb.ToString());
        }

        // ═══════════════════════════════════════════════════════
        //  MONSTER COUNTERATTACK
        // ═══════════════════════════════════════════════════════

        // Druid Entangle — skip counterattack this round
        if (run.MonsterAtk == -1)
        {
            run.MonsterAtk = run.MonsterDef * 2; // restore approximate ATK from DEF ratio
            HandleDeaths(run, sb);
            if (run.Party.Count == 0)
            {
                ActiveDungeons.TryRemove(channelId, out _);
                sb.AppendLine("\n💀 **Party wiped!** Dungeon failed.");
            }
            return (true, sb.ToString());
        }

        // Bard Mockery debuff — 20% ATK reduction this round
        var effectiveMonsterAtk = run.BardMockeryActive ? run.MonsterAtk * 80 / 100 : run.MonsterAtk;
        if (run.BardMockeryActive)
        {
            sb.AppendLine($"🎵 Monster's attack weakened by Vicious Mockery!");
            run.BardMockeryActive = false;
        }

        sb.AppendLine($"\n👹 Monster HP: {run.MonsterHp}");

        // Monster attacks raised skeleton first if it exists
        if (run.SkeletonHp > 0 && _rng.Next(100) < 30)
        {
            var skelDmg = Math.Max(1, effectiveMonsterAtk / 2);
            run.SkeletonHp -= skelDmg;
            sb.AppendLine($"👹 Monster attacks the skeleton for {skelDmg}! (Skeleton HP: {Math.Max(0, run.SkeletonHp)})");
            if (run.SkeletonHp <= 0)
            {
                run.SkeletonHp = 0;
                sb.AppendLine("💀 The raised skeleton crumbles!");
            }
        }
        else
        {
            // Pick a target
            var targetIdx = _rng.Next(run.Party.Count);
            var target = run.Party[targetIdx];
            var monsterDmg = Math.Max(1, effectiveMonsterAtk - target.Defense / 2 + _rng.Next(-3, 5));

            // Paladin Aura — reduce damage to all allies by 10%
            if (run.Party.Any(p => p.Class == "Paladin"))
                monsterDmg = monsterDmg * 90 / 100;

            // ═══ DEFENSIVE CLASS ABILITIES ═══

            // Monk: Evasion (20% full dodge)
            if (target.Class == "Monk" && _rng.Next(100) < 20)
            {
                sb.AppendLine($"👊 {target.Username} **dodges**! (Evasion)");
            }
            // Mage: Arcane Ward (25% absorb)
            else if (target.Class == "Mage" && _rng.Next(100) < 25)
            {
                var absorbed = Math.Min(monsterDmg, 15 + target.Level * 3);
                var remaining = Math.Max(0, monsterDmg - absorbed);
                target.Hp = Math.Max(0, target.Hp - remaining);
                sb.AppendLine($"🔮 **Arcane Ward!** Absorbs {absorbed}! ({remaining} through, {target.Hp}/{target.MaxHp} HP)");
            }
            // Warrior: Second Wind (auto-heal when below 30%)
            else if (target.Class == "Warrior")
            {
                target.Hp = Math.Max(0, target.Hp - monsterDmg);
                sb.AppendLine($"👹 Monster attacks {target.Username} for {monsterDmg}! ({target.Hp}/{target.MaxHp} HP)");
                if (target.Hp > 0 && target.Hp <= target.MaxHp * 30 / 100)
                {
                    var heal = target.MaxHp * 20 / 100;
                    target.Hp = Math.Min(target.MaxHp, target.Hp + heal);
                    sb.AppendLine($"🛡️ **Second Wind!** {target.Username} recovers {heal} HP! ({target.Hp}/{target.MaxHp})");
                }
            }
            // Barbarian: Relentless — survive one lethal hit at 1 HP per dungeon
            else if (target.Class == "Barbarian" && target.Hp - monsterDmg <= 0 && run.RagingPlayers.Contains(target.UserId))
            {
                target.Hp = 1;
                run.RagingPlayers.Remove(target.UserId);
                sb.AppendLine($"⚔️ **Relentless!** {target.Username} refuses to fall! (1 HP — Rage spent)");
            }
            // Cleric: Divine Shield (20% redirect half to cleric)
            else if (target.Class != "Cleric" && run.Party.Any(p => p.Class == "Cleric") && _rng.Next(100) < 20)
            {
                var cleric = run.Party.First(p => p.Class == "Cleric");
                var shielded = monsterDmg / 2;
                target.Hp = Math.Max(0, target.Hp - (monsterDmg - shielded));
                cleric.Hp = Math.Max(0, cleric.Hp - shielded);
                sb.AppendLine($"✝️ **Divine Shield!** {cleric.Username} absorbs {shielded} for {target.Username}!");
            }
            // Paladin: Lay on Hands — 15% to self-heal after taking damage
            else if (target.Class == "Paladin")
            {
                target.Hp = Math.Max(0, target.Hp - monsterDmg);
                sb.AppendLine($"👹 Monster attacks {target.Username} for {monsterDmg}! ({target.Hp}/{target.MaxHp} HP)");
                if (target.Hp > 0 && _rng.Next(100) < 15)
                {
                    var layHeal = 15 + target.Level * 3;
                    target.Hp = Math.Min(target.MaxHp, target.Hp + layHeal);
                    sb.AppendLine($"⚜️ **Lay on Hands!** {target.Username} heals {layHeal} HP! ({target.Hp}/{target.MaxHp})");
                }
            }
            // Druid: Wild Shape absorbs damage
            else if (target.Class == "Druid" && run.WildShapeHp.TryGetValue(target.UserId, out var wsHp) && wsHp > 0)
            {
                var absorbed = Math.Min(monsterDmg, wsHp);
                run.WildShapeHp[target.UserId] = wsHp - absorbed;
                var remaining = monsterDmg - absorbed;
                target.Hp = Math.Max(0, target.Hp - remaining);
                sb.AppendLine($"🌿 **Wild Shape** absorbs {absorbed}! {target.Username} takes {remaining} ({target.Hp}/{target.MaxHp} HP)");
            }
            // Halfling: Lucky — reroll dodge once per dungeon
            else if (target.Race == "Halfling" && !run.LuckyUsed.Contains(target.UserId) && _rng.Next(100) < 50)
            {
                run.LuckyUsed.Add(target.UserId);
                sb.AppendLine($"🍀 **Lucky!** {target.Username} narrowly avoids the blow! (one-time save)");
            }
            else
            {
                target.Hp = Math.Max(0, target.Hp - monsterDmg);
                sb.AppendLine($"👹 Monster attacks {target.Username} for {monsterDmg}! ({target.Hp}/{target.MaxHp} HP)");

                // Tiefling: Infernal Wrath retaliates when below 50%
                if (target.Race == "Tiefling" && target.Hp > 0 && target.Hp <= target.MaxHp / 2)
                {
                    var wrathDmg = 5 + target.Level * 2;
                    run.MonsterHp -= wrathDmg;
                    sb.AppendLine($"😈 **Infernal Wrath!** {target.Username} retaliates for {wrathDmg}!");
                }
            }
        }

        HandleDeaths(run, sb);
        if (run.Party.Count == 0)
        {
            ActiveDungeons.TryRemove(channelId, out _);
            sb.AppendLine("\n💀 **Party wiped!** Dungeon failed.");
        }

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  FLEE
    // ═══════════════════════════════════════════════════════════

    public (bool Success, string Message) Flee(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");

        var fleeChance = 60;
        if (run.Party.Any(p => p.Class == "Rogue")) fleeChance += 15;
        if (run.Party.Any(p => p.Race == "Halfling")) fleeChance += 10;
        if (run.Party.Any(p => p.Class == "Ranger")) fleeChance += 10;

        if (_rng.Next(100) < fleeChance)
        {
            run.CurrentMonster = null;
            return (true, "🏃 Party fled successfully! Use `.dungeon explore` to continue.");
        }

        if (run.Party.Count > 0)
        {
            var targetIdx = _rng.Next(run.Party.Count);
            var target = run.Party[targetIdx];
            var damage = Math.Max(1, run.MonsterAtk - target.Defense / 2);
            target.Hp = Math.Max(0, target.Hp - damage);

            var sb = new StringBuilder();
            sb.AppendLine($"🏃 Flee failed! Monster attacks {target.Username} for {damage}!");

            HandleDeaths(run, sb);
            if (run.Party.Count == 0)
            {
                ActiveDungeons.TryRemove(channelId, out _);
                sb.AppendLine("💀 **Party wiped!**");
            }

            return (true, sb.ToString());
        }

        return (false, "Flee failed!");
    }

    // ═══════════════════════════════════════════════════════════
    //  CLASS & RACE SELECTION
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> ChooseClassAsync(ulong userId, ulong guildId, string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return (false, $"Specify a class! Choose: {string.Join(", ", Classes.Keys.Select(k => $"**{k}**"))}");

        var normalized = className.Trim();
        normalized = char.ToUpper(normalized[0]) + normalized[1..].ToLower();

        if (!Classes.ContainsKey(normalized))
        {
            var valid = string.Join(", ", Classes.Keys.Select(k => $"**{k}**"));
            return (false, $"Invalid class! Choose: {valid}");
        }

        var player = await GetOrCreatePlayerAsync(userId, guildId);
        player.Class = normalized;
        await SavePlayerAsync(player);

        var cls = Classes[normalized];
        return (true, $"{cls.Emoji} Class set to **{normalized}**!\n*{cls.Desc}*\n" +
            $"HP: {cls.HpMult}% | ATK: {cls.AtkMult}% | DEF: {cls.DefMult}%");
    }

    public async Task<(bool Success, string Message)> ChooseRaceAsync(ulong userId, ulong guildId, string raceName)
    {
        if (string.IsNullOrWhiteSpace(raceName))
            return (false, $"Specify a race! Choose: {string.Join(", ", Races.Keys.Select(k => $"**{k}**"))}");

        var normalized = raceName.Trim();
        normalized = char.ToUpper(normalized[0]) + normalized[1..].ToLower();

        if (!Races.ContainsKey(normalized))
        {
            var valid = string.Join(", ", Races.Keys.Select(k => $"**{k}**"));
            return (false, $"Invalid race! Choose: {valid}");
        }

        var player = await GetOrCreatePlayerAsync(userId, guildId);
        player.Race = normalized;
        await SavePlayerAsync(player);

        var race = Races[normalized];
        return (true, $"{race.Emoji} Race set to **{normalized}**!\n*{race.Desc}*\n" +
            $"Racial: **{race.Ability}**\n" +
            $"HP: {race.HpMod}% | ATK: {race.AtkMod}% | DEF: {race.DefMod}%");
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private void HandleDeaths(DungeonRun run, StringBuilder sb)
    {
        var dead = run.Party.Where(p => p.Hp <= 0).ToList();
        foreach (var p in dead)
        {
            // Necromancer: Death Ward — survive once
            if (p.DeathWardActive)
            {
                p.Hp = p.MaxHp / 4;
                p.DeathWardActive = false;
                sb.AppendLine($"💀 **Death Ward!** {p.Username} cheats death! ({p.Hp} HP)");
            }
            else if (p.HasRevive)
            {
                p.Hp = p.MaxHp / 2;
                p.HasRevive = false;
                sb.AppendLine($"🔥 {p.Username}'s **Phoenix Feather** activates! ({p.Hp} HP)");
            }
            else
            {
                sb.AppendLine($"💀 {p.Username} has fallen!");
                run.Party.Remove(p);
            }
        }
    }

    private async Task<(bool Success, string Message)> FinishDungeonAsync(ulong channelId, DungeonRun run, StringBuilder sb)
    {
        var bonusLoot = run.Difficulty * 200;
        run.TotalLoot += bonusLoot;
        var perPlayer = run.TotalLoot / Math.Max(1, run.Party.Count);
        var xpPerPlayer = run.XpPool / Math.Max(1, run.Party.Count);

        // Human: Versatile — +15% XP on completion (once, even with multiple Humans)
        if (run.Party.Any(p => p.Race == "Human"))
            xpPerPlayer = xpPerPlayer * 115 / 100;

        sb.AppendLine($"\n🏆 **DUNGEON COMPLETE!** (Difficulty {run.Difficulty})");
        sb.AppendLine($"Bonus: {bonusLoot} 🥠 | Total: {run.TotalLoot} 🥠");
        sb.AppendLine($"XP: {xpPerPlayer} per player\n");

        foreach (var p in run.Party)
        {
            await _cs.AddAsync(p.UserId, perPlayer, new TxData("dungeon", "loot"));

            var player = await GetOrCreatePlayerAsync(p.UserId, run.GuildId);
            player.Xp += xpPerPlayer;
            player.TotalLoot += perPlayer;
            player.MonstersKilled += p.MonstersKilled;
            player.DungeonsCleared++;
            if (run.Difficulty > player.HighestDifficulty)
                player.HighestDifficulty = run.Difficulty;

            var newLevel = CalculateLevel(player.Xp);
            var levelsGained = newLevel - player.Level;

            if (levelsGained > 0)
            {
                player.Level = newLevel;
                player.BaseHp += levelsGained * 5;
                player.BaseAttack += levelsGained * 2;
                player.BaseDefense += levelsGained * 1;
                sb.AppendLine($"🎉 **{p.Username} leveled up!** Lv.{newLevel - levelsGained} → **Lv.{newLevel}**");
                sb.AppendLine($"   +{levelsGained * 5} HP | +{levelsGained * 2} ATK | +{levelsGained} DEF");
            }

            sb.AppendLine($"  {p.Username}: +{perPlayer} 🥠 | +{xpPerPlayer} XP ({player.Xp}/{XpForLevel(player.Level + 1)} to next)");
            await SavePlayerAsync(player);
        }

        if (run.LootDrops.Count > 0)
        {
            sb.AppendLine($"\n📦 **Loot ({run.LootDrops.Count} items):**");
            foreach (var item in run.LootDrops)
            {
                await SaveItemAsync(item);
                sb.AppendLine($"  {RarityEmoji(item.Rarity)} {item.Name} ({item.Slot}) → inventory");
            }
            sb.AppendLine("Use `.dungeon inventory` and `.dungeon equip <id>`!");
        }

        ActiveDungeons.TryRemove(channelId, out _);

        // Check for random raid boss spawn
        if (OnDungeonClearedAsync is not null)
        {
            var (spawned, raidMsg, raidChannel) = await OnDungeonClearedAsync(run.GuildId);
            if (spawned)
            {
                sb.AppendLine($"\n🚨 **A RAID BOSS HAS APPEARED!** 🚨");
                sb.AppendLine($"Head to <#{raidChannel}> to fight!");
            }
        }

        return (true, sb.ToString());
    }
}
