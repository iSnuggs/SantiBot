#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;
using System.Text;

namespace SantiBot.Modules.Games.Expansion;

public sealed class ExpansionService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    // ═══════════════════════════════════════════════════════════════
    //  SKILL TREE DATA — 3 classes x 5 skills each (15 total)
    //  Other 8 classes (Paladin, Ranger, Necromancer, Bard, Cleric,
    //  Berserker, Assassin, Druid) follow the same pattern.
    // ═══════════════════════════════════════════════════════════════

    public record SkillInfo(string Name, string Description, string Effect);

    // Key: class name -> array of 5 skills
    public static readonly Dictionary<string, SkillInfo[]> ClassSkills = new()
    {
        ["Warrior"] =
        [
            new("Shield Wall", "Raises a mighty barrier of steel.", "+8% Defense per level"),
            new("Cleave", "A sweeping blow that hits all enemies.", "+6% AoE Attack per level"),
            new("Battle Cry", "Inspires allies, striking fear into foes.", "+4% team ATK per level"),
            new("Iron Skin", "Hardens skin to resist blows.", "+10% HP per level"),
            new("Whirlwind", "Spin attack devastating everything nearby.", "+12% ATK, -2% DEF per level"),
        ],
        ["Mage"] =
        [
            new("Fireball", "Hurls a blazing sphere of destruction.", "+10% ATK per level"),
            new("Frost Armor", "Encases the caster in protective ice.", "+8% DEF per level"),
            new("Arcane Surge", "Channels raw arcane power.", "+15% ATK, -5% HP per level"),
            new("Mana Shield", "Converts magical energy into protection.", "+6% HP per level"),
            new("Meteor Strike", "Calls down a devastating meteor.", "+20% ATK on crits per level"),
        ],
        ["Rogue"] =
        [
            new("Backstab", "A precise strike from the shadows.", "+12% ATK per level"),
            new("Evasion", "Dodges incoming attacks with grace.", "+10% DEF per level"),
            new("Poison Blade", "Coats weapons in deadly toxin.", "+5% ATK + DoT per level"),
            new("Shadow Step", "Teleport behind the enemy.", "+8% crit chance per level"),
            new("Assassinate", "A lethal finishing move.", "+25% ATK when enemy < 30% HP per level"),
        ],
        ["Paladin"] =
        [
            new("Holy Strike", "Smites enemies with divine power.", "+9% ATK per level"),
            new("Divine Shield", "A blessed barrier absorbs damage.", "+10% DEF per level"),
            new("Lay on Hands", "Heals wounds through faith.", "+8% HP regen per level"),
            new("Aura of Light", "Bolsters allies near the paladin.", "+4% team DEF per level"),
            new("Judgment", "Delivers righteous punishment.", "+14% ATK vs undead per level"),
        ],
        ["Ranger"] =
        [
            new("Precise Shot", "A carefully aimed arrow.", "+10% ATK per level"),
            new("Camouflage", "Blends into surroundings.", "+7% evasion per level"),
            new("Trap Mastery", "Sets deadly traps for foes.", "+6% ATK + slow per level"),
            new("Eagle Eye", "Enhanced vision reveals weaknesses.", "+8% crit per level"),
            new("Volley", "Rains arrows on a wide area.", "+11% AoE ATK per level"),
        ],
        ["Necromancer"] =
        [
            new("Drain Life", "Siphons health from enemies.", "+7% ATK + heal per level"),
            new("Bone Armor", "Surrounds caster with skeletal shields.", "+9% DEF per level"),
            new("Summon Undead", "Raises a skeleton to fight.", "+5% companion ATK per level"),
            new("Curse of Weakness", "Reduces enemy stats.", "-6% enemy ATK per level"),
            new("Death Nova", "Explodes with necrotic energy.", "+18% ATK, -4% HP per level"),
        ],
        ["Bard"] =
        [
            new("Inspiring Melody", "A song that boosts allies.", "+5% team ATK per level"),
            new("Soothing Ballad", "A calming tune that heals.", "+6% HP regen per level"),
            new("Discordant Note", "A jarring sound damages foes.", "+8% ATK per level"),
            new("War Drums", "Rhythmic beats increase combat speed.", "+7% ATK speed per level"),
            new("Encore", "Repeats the last ability used.", "+4% all stats per level"),
        ],
        ["Cleric"] =
        [
            new("Smite", "Calls divine wrath on enemies.", "+8% ATK per level"),
            new("Heal", "Restores health through prayer.", "+10% HP regen per level"),
            new("Sanctuary", "Creates a protective holy zone.", "+9% DEF per level"),
            new("Blessing", "Increases an ally's power.", "+5% team stats per level"),
            new("Resurrect", "Prevents a lethal blow once.", "+6% survival per level"),
        ],
        ["Berserker"] =
        [
            new("Rage", "Enters a furious state.", "+15% ATK, -8% DEF per level"),
            new("Reckless Swing", "A wild, powerful attack.", "+12% ATK per level"),
            new("Blood Frenzy", "Grows stronger as HP drops.", "+10% ATK when low HP per level"),
            new("Thick Skull", "Resists stuns and knockdowns.", "+6% stun resist per level"),
            new("Rampage", "Unstoppable chain of blows.", "+20% ATK, -10% DEF per level"),
        ],
        ["Assassin"] =
        [
            new("Ambush", "Opens combat from hiding.", "+14% first-strike ATK per level"),
            new("Smoke Bomb", "Blinds enemies reducing accuracy.", "+8% evasion per level"),
            new("Vital Strike", "Targets weak points.", "+10% crit damage per level"),
            new("Vanish", "Disappears mid-combat.", "+7% DEF per level"),
            new("Death Mark", "Marks a target for death.", "+16% ATK vs marked per level"),
        ],
        ["Druid"] =
        [
            new("Entangle", "Vines restrain enemies.", "+6% slow + ATK per level"),
            new("Bark Skin", "Hardens skin like tree bark.", "+9% DEF per level"),
            new("Rejuvenation", "Nature gradually heals wounds.", "+8% HP regen per level"),
            new("Summon Beast", "Calls a wild animal ally.", "+5% companion ATK per level"),
            new("Wrath of Nature", "Unleashes nature's fury.", "+13% ATK per level"),
        ],
    };

    // ═══════════════════════════════════════════════════════════════
    //  SKILL TREE METHODS
    // ═══════════════════════════════════════════════════════════════

    public async Task<SkillTree> GetOrCreateSkillTreeAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var tree = await ctx.GetTable<SkillTree>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (tree is not null)
            return tree;

        // Check what class the player has chosen in the dungeon system
        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        var playerClass = player?.Class ?? "Warrior";

        tree = new SkillTree
        {
            UserId = userId,
            GuildId = guildId,
            Class = playerClass,
            SkillPoints = 0,
        };

        ctx.Set<SkillTree>().Add(tree);
        await ctx.SaveChangesAsync();
        return tree;
    }

    /// <summary>
    /// Calculate unspent skill points: 1 point per 5 dungeon levels, minus already spent.
    /// </summary>
    public async Task<int> CalculateAvailableSkillPointsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        var tree = await ctx.GetTable<SkillTree>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (player is null || tree is null)
            return 0;

        var totalEarned = player.Level / 5;
        var totalSpent = tree.Skill1Level + tree.Skill2Level + tree.Skill3Level
                       + tree.Skill4Level + tree.Skill5Level;

        return totalEarned - totalSpent;
    }

    /// <summary>
    /// Upgrade a skill (1-5) by one level. Returns (success, errorReason).
    /// </summary>
    public async Task<(bool success, string error)> UpgradeSkillAsync(ulong userId, ulong guildId, int skillNum)
    {
        if (skillNum < 1 || skillNum > 5)
            return (false, "Skill number must be 1-5.");

        var available = await CalculateAvailableSkillPointsAsync(userId, guildId);
        if (available <= 0)
            return (false, "No skill points available. Earn more by leveling in dungeons (1 point per 5 levels).");

        await using var ctx = _db.GetDbContext();
        var tree = await ctx.GetTable<SkillTree>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (tree is null)
            return (false, "No skill tree found. Use the skill tree command first.");

        var currentLevel = skillNum switch
        {
            1 => tree.Skill1Level,
            2 => tree.Skill2Level,
            3 => tree.Skill3Level,
            4 => tree.Skill4Level,
            5 => tree.Skill5Level,
            _ => 0,
        };

        if (currentLevel >= 5)
            return (false, "That skill is already at max level (5).");

        // Build the update dynamically based on skill number
        switch (skillNum)
        {
            case 1:
                await ctx.GetTable<SkillTree>()
                    .Where(x => x.Id == tree.Id)
                    .UpdateAsync(_ => new SkillTree { Skill1Level = tree.Skill1Level + 1 });
                break;
            case 2:
                await ctx.GetTable<SkillTree>()
                    .Where(x => x.Id == tree.Id)
                    .UpdateAsync(_ => new SkillTree { Skill2Level = tree.Skill2Level + 1 });
                break;
            case 3:
                await ctx.GetTable<SkillTree>()
                    .Where(x => x.Id == tree.Id)
                    .UpdateAsync(_ => new SkillTree { Skill3Level = tree.Skill3Level + 1 });
                break;
            case 4:
                await ctx.GetTable<SkillTree>()
                    .Where(x => x.Id == tree.Id)
                    .UpdateAsync(_ => new SkillTree { Skill4Level = tree.Skill4Level + 1 });
                break;
            case 5:
                await ctx.GetTable<SkillTree>()
                    .Where(x => x.Id == tree.Id)
                    .UpdateAsync(_ => new SkillTree { Skill5Level = tree.Skill5Level + 1 });
                break;
        }

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRESTIGE SYSTEM
    // ═══════════════════════════════════════════════════════════════

    public async Task<PrestigeData> GetPrestigeAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PrestigeData>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);
    }

    /// <summary>
    /// Prestige: reset dungeon level to 1, gain +5% permanent stat bonus.
    /// Requires level 50+. Max prestige 20 (100% bonus).
    /// </summary>
    // Prevent double-prestige from concurrent calls
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong, ulong), byte> _prestigeLocks = new();

    public async Task<(bool success, string error, int newPrestige)> PrestigeAsync(ulong userId, ulong guildId)
    {
        // Atomic lock — only one prestige operation per user at a time
        if (!_prestigeLocks.TryAdd((userId, guildId), 0))
            return (false, "Prestige already in progress, please wait.", 0);

        try
        {
        await using var ctx = _db.GetDbContext();

        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (player is null)
            return (false, "You need a dungeon character first! Run a dungeon to get started.", 0);

        if (player.Level < 50)
            return (false, $"You need to be at least level 50 to prestige. You are level {player.Level}.", 0);

        var prestige = await ctx.GetTable<PrestigeData>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        var currentPrestige = prestige?.PrestigeLevel ?? 0;
        if (currentPrestige >= 20)
            return (false, "You have reached the maximum prestige level (20)!", 0);

        var newPrestige = currentPrestige + 1;
        var newBonus = newPrestige * 5;

        // Reset player level to 1 and base stats
        await ctx.GetTable<DungeonPlayer>()
            .Where(x => x.Id == player.Id)
            .UpdateAsync(_ => new DungeonPlayer
            {
                Level = 1,
                Xp = 0,
                BaseHp = 100,
                BaseAttack = 20,
                BaseDefense = 10,
            });

        // Reset skill tree
        var tree = await ctx.GetTable<SkillTree>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);
        if (tree is not null)
        {
            await ctx.GetTable<SkillTree>()
                .Where(x => x.Id == tree.Id)
                .UpdateAsync(_ => new SkillTree
                {
                    Skill1Level = 0,
                    Skill2Level = 0,
                    Skill3Level = 0,
                    Skill4Level = 0,
                    Skill5Level = 0,
                });
        }

        // Update or create prestige record
        if (prestige is not null)
        {
            await ctx.GetTable<PrestigeData>()
                .Where(x => x.Id == prestige.Id)
                .UpdateAsync(_ => new PrestigeData
                {
                    PrestigeLevel = newPrestige,
                    PrestigeBonusPercent = newBonus,
                    LastPrestigeAt = DateTime.UtcNow,
                });
        }
        else
        {
            ctx.Add(new PrestigeData
            {
                UserId = userId,
                GuildId = guildId,
                PrestigeLevel = newPrestige,
                PrestigeBonusPercent = newBonus,
                LastPrestigeAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        return (true, null, newPrestige);
        }
        finally
        {
            _prestigeLocks.TryRemove((userId, guildId), out _);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DUNGEON MODIFIERS — 15 modifiers with distinct effects
    // ═══════════════════════════════════════════════════════════════

    public record ModifierTemplate(string Name, string Description, double AtkMult, double DefMult, double HpMult, double XpMult, double LootMult);

    public static readonly ModifierTemplate[] AllModifiers =
    [
        new("Double XP",        "Experience gains are doubled!",                  1.0, 1.0, 1.0, 2.0, 1.0),
        new("Glass Cannon",     "2x Attack but only half HP.",                    2.0, 1.0, 0.5, 1.0, 1.0),
        new("Loot Frenzy",      "Treasure drops are tripled!",                    1.0, 1.0, 1.0, 1.0, 3.0),
        new("Poison Fog",       "Toxic air reduces HP by 30%.",                   1.0, 1.0, 0.7, 1.0, 1.0),
        new("Dark Rooms",       "Poor visibility reduces accuracy by 25%.",       0.75, 1.0, 1.0, 1.0, 1.0),
        new("Monster Surge",    "Double enemies but double XP!",                  1.0, 1.0, 1.0, 2.0, 1.0),
        new("Blessed",          "Divine favor boosts all stats by 20%.",           1.2, 1.2, 1.2, 1.2, 1.2),
        new("Cursed",           "Dark energy reduces all stats by 15%.",           0.85, 0.85, 0.85, 0.85, 0.85),
        new("Treasure Vault",   "A hidden vault doubles loot!",                   1.0, 1.0, 1.0, 1.0, 2.0),
        new("Berserker Rage",   "50% more ATK, 30% less DEF.",                    1.5, 0.7, 1.0, 1.0, 1.0),
        new("Fortified",        "Enemies are tougher but drop more XP.",          1.0, 1.5, 1.0, 1.5, 1.0),
        new("Featherweight",    "Move fast: +30% ATK, -20% HP.",                  1.3, 1.0, 0.8, 1.0, 1.0),
        new("Golden Hour",      "Everything gives 50% more loot.",                1.0, 1.0, 1.0, 1.0, 1.5),
        new("Iron Will",        "Defense doubled but attack halved.",              0.5, 2.0, 1.0, 1.0, 1.0),
        new("Chaos Dungeon",    "All multipliers randomized (0.5x - 2.0x)!",     1.0, 1.0, 1.0, 1.0, 1.0),
    ];

    /// <summary>
    /// Get currently active modifiers for a guild.
    /// </summary>
    public async Task<List<DungeonModifier>> GetActiveModifiersAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<DungeonModifier>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Roll new random modifiers for a guild (1-3 active at once).
    /// </summary>
    public async Task<List<DungeonModifier>> RollNewModifiersAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Deactivate all current modifiers
        await ctx.GetTable<DungeonModifier>()
            .Where(x => x.GuildId == guildId)
            .UpdateAsync(_ => new DungeonModifier { IsActive = false });

        // Pick 1-3 random modifiers
        var count = _rng.Next(1, 4);
        var picked = AllModifiers.OrderBy(_ => _rng.Next()).Take(count).ToList();
        var results = new List<DungeonModifier>();

        foreach (var template in picked)
        {
            double atkMult = template.AtkMult, defMult = template.DefMult;
            double hpMult = template.HpMult, xpMult = template.XpMult, lootMult = template.LootMult;

            // For Chaos Dungeon, randomize all multipliers
            if (template.Name == "Chaos Dungeon")
            {
                atkMult = 0.5 + _rng.Next(0, 16) * 0.1;
                defMult = 0.5 + _rng.Next(0, 16) * 0.1;
                hpMult = 0.5 + _rng.Next(0, 16) * 0.1;
                xpMult = 0.5 + _rng.Next(0, 16) * 0.1;
                lootMult = 0.5 + _rng.Next(0, 16) * 0.1;
            }

            var mod = new DungeonModifier
            {
                GuildId = guildId,
                ModifierName = template.Name,
                Description = template.Description,
                IsActive = true,
                AtkMult = atkMult,
                DefMult = defMult,
                HpMult = hpMult,
                XpMult = xpMult,
                LootMult = lootMult,
            };

            ctx.Set<DungeonModifier>().Add(mod);
            results.Add(mod);
        }

        await ctx.SaveChangesAsync();
        return results;
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOUNTY BOARD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Post a bounty on a user. Costs the poster the bounty amount upfront.
    /// </summary>
    public async Task<(bool success, string error)> PostBountyAsync(
        ulong guildId, ulong posterId, ulong targetId, long amount, string reason)
    {
        if (amount < 100)
            return (false, "Minimum bounty is 100 currency.");

        if (posterId == targetId)
            return (false, "You can't put a bounty on yourself!");

        var taken = await _cs.RemoveAsync(posterId, amount, new("bounty", "post", "Posted a bounty"));
        if (!taken)
            return (false, "You don't have enough currency to post this bounty.");

        await using var ctx = _db.GetDbContext();

        // Check if there's already an active bounty on this target
        var existing = await ctx.GetTable<Bounty>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.TargetUserId == targetId && !x.IsClaimed);

        if (existing is not null)
        {
            // Stack bounties — add to existing amount
            await ctx.GetTable<Bounty>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new Bounty { Amount = existing.Amount + amount });
            return (true, null);
        }

        ctx.Set<Bounty>().Add(new Bounty
        {
            GuildId = guildId,
            TargetUserId = targetId,
            PostedBy = posterId,
            Amount = amount,
            Reason = reason ?? "No reason given.",
            IsClaimed = false,
            PostedAt = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Get all open bounties in a guild.
    /// </summary>
    public async Task<List<Bounty>> GetOpenBountiesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<Bounty>()
            .Where(x => x.GuildId == guildId && !x.IsClaimed)
            .OrderByDescending(x => x.Amount)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Claim a bounty by defeating the target in PvP. The claimer must have higher
    /// dungeon stats than the target. Returns (success, error, amount won).
    /// </summary>
    public async Task<(bool success, string error, long amount)> ClaimBountyAsync(
        ulong guildId, ulong claimerId, ulong targetId)
    {
        if (claimerId == targetId)
            return (false, "You can't claim a bounty on yourself!", 0);

        await using var ctx = _db.GetDbContext();

        var bounty = await ctx.GetTable<Bounty>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.TargetUserId == targetId && !x.IsClaimed);

        if (bounty is null)
            return (false, "No active bounty on that user.", 0);

        // PvP combat check — compare dungeon stats
        var claimer = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsync(x => x.UserId == claimerId && x.GuildId == guildId);
        var target = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsync(x => x.UserId == targetId && x.GuildId == guildId);

        if (claimer is null)
            return (false, "You need a dungeon character to claim bounties!", 0);
        if (target is null)
            return (false, "The target doesn't have a dungeon character.", 0);

        // Combat: compare power (ATK + DEF + HP/10) with some randomness
        var claimerPower = claimer.BaseAttack + claimer.BaseDefense + claimer.BaseHp / 10
                         + _rng.Next(-10, 11);
        var targetPower = target.BaseAttack + target.BaseDefense + target.BaseHp / 10
                        + _rng.Next(-10, 11);

        if (claimerPower <= targetPower)
            return (false, $"You challenged the bounty target but lost the fight! (Your power: {claimerPower} vs theirs: {targetPower})", 0);

        // Claim the bounty
        await ctx.GetTable<Bounty>()
            .Where(x => x.Id == bounty.Id)
            .UpdateAsync(_ => new Bounty
            {
                IsClaimed = true,
                ClaimedBy = claimerId,
                ClaimedAt = DateTime.UtcNow,
            });

        await _cs.AddAsync(claimerId, bounty.Amount, new("bounty", "claim", "Claimed a bounty"));

        return (true, null, bounty.Amount);
    }

    // ═══════════════════════════════════════════════════════════════
    //  TREASURE HUNTS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Admin hides a word in a channel. First person to type it wins.
    /// </summary>
    public async Task<(bool success, string error)> HideTreasureAsync(
        ulong guildId, ulong channelId, string word, long reward)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
            return (false, "The hidden word must be at least 3 characters.");

        if (reward < 50)
            return (false, "Minimum treasure reward is 50 currency.");

        await using var ctx = _db.GetDbContext();

        // Check for existing unfound treasure in this channel
        var existing = await ctx.GetTable<TreasureHunt>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.ChannelId == channelId && !x.IsFound);

        if (existing is not null)
            return (false, "There's already an active treasure hunt in this channel!");

        ctx.Set<TreasureHunt>().Add(new TreasureHunt
        {
            GuildId = guildId,
            ChannelId = channelId,
            HiddenWord = word.ToLowerInvariant(),
            Reward = reward,
            IsFound = false,
            HiddenAt = DateTime.UtcNow,
        });

        await ctx.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Check if a message contains the treasure word. Returns (found, reward) if match.
    /// </summary>
    public async Task<(bool found, long reward, TreasureHunt hunt)> CheckTreasureAsync(
        ulong guildId, ulong channelId, ulong userId, string message)
    {
        await using var ctx = _db.GetDbContext();

        var hunt = await ctx.GetTable<TreasureHunt>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.ChannelId == channelId && !x.IsFound);

        if (hunt is null)
            return (false, 0, null);

        if (!message.ToLowerInvariant().Contains(hunt.HiddenWord))
            return (false, 0, null);

        // Found it!
        await ctx.GetTable<TreasureHunt>()
            .Where(x => x.Id == hunt.Id)
            .UpdateAsync(_ => new TreasureHunt
            {
                IsFound = true,
                FoundBy = userId,
            });

        await _cs.AddAsync(userId, hunt.Reward, new("treasure", "found", "Found hidden treasure"));

        return (true, hunt.Reward, hunt);
    }

    /// <summary>
    /// Get a hint for the active treasure (first and last letter, length).
    /// </summary>
    public async Task<string> GetTreasureHintAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();
        var hunt = await ctx.GetTable<TreasureHunt>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && x.ChannelId == channelId && !x.IsFound);

        if (hunt is null)
            return null;

        var word = hunt.HiddenWord;
        if (word.Length <= 2)
            return $"The word is {word.Length} characters long.";

        var middle = new string('_', word.Length - 2);
        return $"Hint: `{word[0]}{middle}{word[^1]}` ({word.Length} letters)";
    }

    // ═══════════════════════════════════════════════════════════════
    //  HOROSCOPE — 12 signs, 5 categories, deterministic by date
    // ═══════════════════════════════════════════════════════════════

    public static readonly string[] ZodiacSigns =
    [
        "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    ];

    private static readonly string[][] _loveReadings =
    [
        // 0-4: bad to great for each sign seed
        [
            "Romance feels distant today. Focus on self-love and the rest will follow.",
            "A small gesture of kindness opens a door you didn't expect.",
            "The stars align for flirty conversations. Be bold!",
            "A deep connection forms with someone unexpected. Stay open.",
            "Love is everywhere you look! Your charm is absolutely magnetic today.",
        ],
        [
            "Guard your heart carefully today. Not everyone has good intentions.",
            "An old friend might reveal new feelings. Tread carefully.",
            "Passion simmers beneath the surface. Let it build naturally.",
            "A romantic surprise awaits if you step out of your comfort zone.",
            "Today is THE day for grand romantic gestures. Go all in!",
        ],
        [
            "Communication breakdowns in love. Take a step back and breathe.",
            "A compliment from a stranger brightens your entire week.",
            "Your partner (or crush) has something important to say. Listen well.",
            "Date night is written in the stars. Plan something special!",
            "Soulmate energy is radiating from you. People can't help but be drawn close.",
        ],
        [
            "Emotional walls are high today. It's okay to need space.",
            "A sweet message arrives when you least expect it.",
            "Vulnerability is your superpower today. Show your true feelings.",
            "A relationship milestone approaches. Celebrate the journey!",
            "Your love life transforms in a beautiful, unexpected way today.",
        ],
        [
            "Jealousy could rear its head. Trust is more important than ever.",
            "Small acts of devotion mean more than grand declarations today.",
            "Flirting comes naturally — use this energy wisely!",
            "A love letter (or DM) changes everything. Send or receive?",
            "Twin flames burn bright today. Your connection is unbreakable.",
        ],
    ];

    private static readonly string[][] _careerReadings =
    [
        [
            "Work feels like an uphill battle. Pace yourself.",
            "A coworker offers help you didn't know you needed.",
            "A project finally starts coming together. Keep pushing!",
            "Recognition for your hard work arrives at last.",
            "You're unstoppable! Career breakthroughs happen left and right.",
        ],
        [
            "Avoid big decisions at work today. Sleep on it.",
            "A mentor figure appears with valuable wisdom.",
            "Your creative ideas catch someone important's attention.",
            "Promotion energy is strong. Put your best foot forward.",
            "Dream job vibes! The universe is aligning your professional path.",
        ],
        [
            "Burnout warning: take breaks and hydrate.",
            "Networking pays off in a small but meaningful way.",
            "A skill you thought was useless becomes incredibly relevant.",
            "A side project could become a main hustle. Explore it!",
            "Financial abundance flows from your career moves today. Cash in!",
        ],
        [
            "Office drama might pull you in. Stay neutral.",
            "An email or message brings surprisingly good news.",
            "Your leadership qualities shine in a group setting.",
            "A risk at work pays off handsomely. Trust your gut.",
            "You're the MVP today. Everyone notices your contributions!",
        ],
        [
            "Imposter syndrome hits hard. Remember: you earned your place.",
            "A learning opportunity disguises itself as a challenge.",
            "Team collaboration produces something amazing today.",
            "A pay raise or bonus is cosmically supported. Ask for it!",
            "Career of a lifetime energy! Major doors swing wide open.",
        ],
    ];

    private static readonly string[][] _healthReadings =
    [
        [
            "Energy is low. Prioritize rest over productivity today.",
            "A short walk does wonders for your state of mind.",
            "Your body is recovering well. Keep up the good habits!",
            "Peak physical performance today. Hit the gym or go for a run!",
            "You're glowing with vitality! Health has never felt this good.",
        ],
        [
            "Watch what you eat today — your stomach is sensitive.",
            "Stretching and deep breathing bring unexpected relief.",
            "A new health routine starts showing results. Stick with it!",
            "Your immune system is a fortress today. Nothing gets through.",
            "Mind, body, and spirit are in perfect harmony. Cherish this feeling!",
        ],
        [
            "Headache incoming. Drink water and dim the screens.",
            "An afternoon nap recharges you better than any coffee.",
            "Balance is key today. Don't skip meals.",
            "You wake up feeling refreshed and ready for anything.",
            "Athletic energy surges through you. Break a personal record!",
        ],
        [
            "Stress manifests physically. Address the root cause.",
            "A healthy meal choice leads to a surprisingly great mood.",
            "Mental health deserves attention. Journal or talk to someone.",
            "Sleep quality improves dramatically starting tonight.",
            "You feel ten years younger today. Radiating pure wellness!",
        ],
        [
            "Allergies or minor irritations test your patience.",
            "Hydration is your secret weapon. Drink up!",
            "Yoga or meditation brings profound inner peace.",
            "Recovery from recent stress is nearly complete. Almost there!",
            "Boundless energy! You could conquer mountains today.",
        ],
    ];

    private static readonly string[][] _luckReadings =
    [
        [
            "Bad luck lurks around corners. Double-check everything.",
            "A coin flip goes your way. Small wins add up!",
            "Lucky number vibes today. Pay attention to repeating digits.",
            "Fortune smiles on bold moves. Take a calculated risk!",
            "Everything you touch turns to gold! Lottery ticket energy!",
        ],
        [
            "Murphy's Law is in full effect. Plan for contingencies.",
            "A lost item turns up in the most unexpected place.",
            "Good timing puts you in the right place at the right moment.",
            "A gamble pays off! The odds were in your favor.",
            "Jackpot energy! The universe conspires in your favor all day.",
        ],
        [
            "Avoid games of chance today. The stars aren't aligned.",
            "A friend brings unexpected good fortune your way.",
            "Serendipity strikes! A happy accident leads somewhere great.",
            "Lucky streaks continue. Ride the wave!",
            "Once-in-a-lifetime luck. Whatever you try, it works!",
        ],
        [
            "Clumsy energy today. Watch your step (literally).",
            "Finding money on the ground? It's more likely than you think.",
            "The universe sends you a lucky sign. Watch for it!",
            "A contest or raffle tilts in your direction. Enter one!",
            "Legendary luck! You couldn't fail today if you tried.",
        ],
        [
            "Electronics might glitch on you. Save your work often.",
            "A wrong turn leads to a delightful discovery.",
            "Lucky in love AND money today. Rare cosmic alignment!",
            "A wish you forgot about starts coming true.",
            "Maximum fortune! Everything breaks perfectly in your direction!",
        ],
    ];

    private static readonly string[][] _adventureReadings =
    [
        [
            "Stay close to home today. The dungeon can wait.",
            "A short expedition yields a surprising find.",
            "The path ahead clears. Push deeper into the unknown!",
            "Epic loot energy! Your next dungeon run will be legendary.",
            "Destiny calls! A world-changing adventure begins today.",
        ],
        [
            "Traps and pitfalls abound. Proceed with extreme caution.",
            "An NPC (friend) shares a map to hidden treasure.",
            "Your party synergy is excellent today. Group up!",
            "A boss fight goes smoother than expected. Victory!",
            "You find a legendary artifact that changes everything!",
        ],
        [
            "Monster encounters are tougher than usual. Level up first.",
            "A secret passage reveals itself to the observant explorer.",
            "Side quests yield better rewards than the main path today.",
            "Critical hit energy! Your attacks land perfectly.",
            "World-first achievement unlocked! History remembers your name.",
        ],
        [
            "Your gear feels inadequate. Time to upgrade before venturing out.",
            "A traveling merchant offers a deal too good to pass up.",
            "Exploration reveals a new area with bountiful resources.",
            "You rally an entire server to conquer the impossible!",
            "The final boss trembles at your approach. Absolute power!",
        ],
        [
            "Wanderlust is strong but preparation is weak. Plan first.",
            "An old map leads to forgotten treasure. Follow the clues!",
            "Perfect weather for adventure. The dungeons await!",
            "You discover a shortcut that saves hours of grinding.",
            "Cosmic adventurer energy! Every quest completes first try!",
        ],
    ];

    /// <summary>
    /// Get a deterministic horoscope for a given sign and date.
    /// Uses the date as a seed so everyone gets the same reading per day per sign.
    /// </summary>
    public HoroscopeReading GetDailyHoroscope(string sign)
    {
        var signIndex = Array.FindIndex(ZodiacSigns, z =>
            z.Equals(sign, StringComparison.OrdinalIgnoreCase));

        if (signIndex < 0)
            return null;

        var today = DateTime.UtcNow.Date;
        var seed = today.Year * 10000 + today.Month * 100 + today.Day + signIndex * 7919;
        var seeded = new Random(seed);

        // Each category gets a 0-4 rating derived from the seed
        var loveRating = seeded.Next(0, 5);
        var careerRating = seeded.Next(0, 5);
        var healthRating = seeded.Next(0, 5);
        var luckRating = seeded.Next(0, 5);
        var adventureRating = seeded.Next(0, 5);

        // Pick reading variant (rotate through the 5 variant arrays)
        var variant = (signIndex + today.DayOfYear) % 5;

        return new HoroscopeReading
        {
            Sign = ZodiacSigns[signIndex],
            Date = today,
            Love = _loveReadings[variant][loveRating],
            LoveRating = loveRating + 1,
            Career = _careerReadings[variant][careerRating],
            CareerRating = careerRating + 1,
            Health = _healthReadings[variant][healthRating],
            HealthRating = healthRating + 1,
            Luck = _luckReadings[variant][luckRating],
            LuckRating = luckRating + 1,
            Adventure = _adventureReadings[variant][adventureRating],
            AdventureRating = adventureRating + 1,
            OverallRating = (loveRating + careerRating + healthRating + luckRating + adventureRating + 5) / 5,
        };
    }

    public async Task<string> GetUserSignAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var h = await ctx.GetTable<Horoscope>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);
        return h?.ZodiacSign;
    }

    public async Task SetUserSignAsync(ulong userId, ulong guildId, string sign)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<Horoscope>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (existing is not null)
        {
            await ctx.GetTable<Horoscope>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new Horoscope
                {
                    ZodiacSign = sign,
                    LastReadingAt = DateTime.UtcNow,
                });
        }
        else
        {
            ctx.Set<Horoscope>().Add(new Horoscope
            {
                UserId = userId,
                GuildId = guildId,
                ZodiacSign = sign,
                LastReadingAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }
    }

    public class HoroscopeReading
    {
        public string Sign { get; set; }
        public DateTime Date { get; set; }
        public string Love { get; set; }
        public int LoveRating { get; set; }
        public string Career { get; set; }
        public int CareerRating { get; set; }
        public string Health { get; set; }
        public int HealthRating { get; set; }
        public string Luck { get; set; }
        public int LuckRating { get; set; }
        public string Adventure { get; set; }
        public int AdventureRating { get; set; }
        public int OverallRating { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  GOAL TRACKER
    // ═══════════════════════════════════════════════════════════════

    public async Task<(bool success, string error)> SetGoalAsync(
        ulong userId, ulong guildId, string name, int target, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Goal name is required.");
        if (target < 1)
            return (false, "Target value must be at least 1.");

        await using var ctx = _db.GetDbContext();

        // Check for existing goal with same name
        var existing = await ctx.GetTable<GoalTracker>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId
                && x.GoalName == name && !x.IsComplete);

        if (existing is not null)
            return (false, $"You already have an active goal named '{name}'. Complete or delete it first.");

        // Limit to 10 active goals
        var activeCount = await ctx.GetTable<GoalTracker>()
            .CountAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId && !x.IsComplete);

        if (activeCount >= 10)
            return (false, "You can only have 10 active goals at a time.");

        ctx.Set<GoalTracker>().Add(new GoalTracker
        {
            UserId = userId,
            GuildId = guildId,
            GoalName = name,
            Description = description ?? "No description.",
            TargetValue = target,
            CurrentValue = 0,
            IsComplete = false,
            Deadline = null,
        });

        await ctx.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, string error, GoalTracker goal)> UpdateGoalProgressAsync(
        ulong userId, ulong guildId, string name, int amount)
    {
        await using var ctx = _db.GetDbContext();
        var goal = await ctx.GetTable<GoalTracker>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId
                && x.GoalName == name && !x.IsComplete);

        if (goal is null)
            return (false, $"No active goal named '{name}' found.", null);

        var newValue = Math.Min(goal.CurrentValue + amount, goal.TargetValue);
        var isComplete = newValue >= goal.TargetValue;

        await ctx.GetTable<GoalTracker>()
            .Where(x => x.Id == goal.Id)
            .UpdateAsync(_ => new GoalTracker
            {
                CurrentValue = newValue,
                IsComplete = isComplete,
            });

        goal.CurrentValue = newValue;
        goal.IsComplete = isComplete;

        return (true, null, goal);
    }

    public async Task<List<GoalTracker>> GetGoalsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<GoalTracker>()
            .Where(x => x.UserId == userId && x.GuildId == guildId)
            .OrderByDescending(x => x.DateAdded)
            .Take(20)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════════
    //  SERVER NEWSPAPER
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Generate a server newspaper digest with stats and highlights.
    /// </summary>
    public async Task<ServerNewspaper> GenerateNewspaperAsync(ulong guildId, ulong generatedBy,
        string topPosters, string topEvents, string newMembers, int memberCount)
    {
        await using var ctx = _db.GetDbContext();

        // Get latest edition number
        var lastEdition = await ctx.GetTable<ServerNewspaper>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Edition)
            .FirstOrDefaultAsync();

        var edition = (lastEdition?.Edition ?? 0) + 1;
        var today = DateTime.UtcNow;

        // Build the newspaper content
        var sb = new StringBuilder();
        sb.AppendLine($"# The Server Times - Edition #{edition}");
        sb.AppendLine($"*Published {today:MMMM dd, yyyy} at {today:HH:mm} UTC*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Server Stats");
        sb.AppendLine($"Total Members: **{memberCount}**");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(topPosters))
        {
            sb.AppendLine("## Top Chatters This Week");
            sb.AppendLine(topPosters);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(topEvents))
        {
            sb.AppendLine("## Recent Events & Achievements");
            sb.AppendLine(topEvents);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(newMembers))
        {
            sb.AppendLine("## Welcome New Members!");
            sb.AppendLine(newMembers);
            sb.AppendLine();
        }

        // Add dungeon leaderboard info
        var topPlayers = await ctx.GetTable<DungeonPlayer>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Level)
            .Take(5)
            .ToListAsyncLinqToDB();

        if (topPlayers.Count > 0)
        {
            sb.AppendLine("## Dungeon Leaderboard");
            for (var i = 0; i < topPlayers.Count; i++)
            {
                var p = topPlayers[i];
                var medal = i switch { 0 => "1st", 1 => "2nd", 2 => "3rd", _ => $"{i + 1}th" };
                sb.AppendLine($"{medal} - <@{p.UserId}> | Level {p.Level} {p.Class} | {p.DungeonsCleared} dungeons cleared");
            }
            sb.AppendLine();
        }

        // Active bounties
        var bounties = await ctx.GetTable<Bounty>()
            .Where(x => x.GuildId == guildId && !x.IsClaimed)
            .OrderByDescending(x => x.Amount)
            .Take(3)
            .ToListAsyncLinqToDB();

        if (bounties.Count > 0)
        {
            sb.AppendLine("## Most Wanted (Bounty Board)");
            foreach (var b in bounties)
                sb.AppendLine($"- <@{b.TargetUserId}> | Bounty: **{b.Amount}** | {b.Reason}");
            sb.AppendLine();
        }

        // Active modifiers
        var mods = await ctx.GetTable<DungeonModifier>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();

        if (mods.Count > 0)
        {
            sb.AppendLine("## Active Dungeon Modifiers");
            foreach (var m in mods)
                sb.AppendLine($"- **{m.ModifierName}**: {m.Description}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*Generated by SantiBot Newspaper System*");

        var content = sb.ToString();

        var paper = new ServerNewspaper
        {
            GuildId = guildId,
            Edition = edition,
            Content = content,
            PublishedAt = today,
            GeneratedBy = generatedBy,
        };

        ctx.Set<ServerNewspaper>().Add(paper);
        await ctx.SaveChangesAsync();
        return paper;
    }

    /// <summary>
    /// Get the latest newspaper edition for a guild.
    /// </summary>
    public async Task<ServerNewspaper> GetLatestNewspaperAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ServerNewspaper>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Edition)
            .FirstOrDefaultAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPER — Star rating display
    // ═══════════════════════════════════════════════════════════════

    public static string StarRating(int rating, int max = 5)
    {
        var filled = Math.Clamp(rating, 0, max);
        var empty = max - filled;
        return new string('*', filled) + new string('.', empty);
    }
}
