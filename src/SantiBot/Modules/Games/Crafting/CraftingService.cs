#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Crafting;

public sealed class CraftingService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    // ─── XP / Level constants ───
    private const int MAX_LEVEL = 50;
    private const int GATHER_COOLDOWN_SECONDS = 60;

    public static int XpForLevel(int level) => level * 50 + 25;

    // ─── Gathering Loot Tables ───
    public record GatherDrop(string ItemName, string ItemType, string Rarity, int MinLevel, int XpReward);

    public static readonly GatherDrop[] OreLootTable =
    [
        new("Copper Ore", "Ore", "Common", 1, 15),
        new("Iron Ore", "Ore", "Common", 5, 25),
        new("Silver Ore", "Ore", "Uncommon", 10, 40),
        new("Gold Ore", "Ore", "Uncommon", 18, 60),
        new("Mithril Ore", "Ore", "Rare", 25, 85),
        new("Adamantite Ore", "Ore", "Epic", 35, 120),
        new("Orichalcum Ore", "Ore", "Legendary", 45, 175),
    ];

    public static readonly GatherDrop[] WoodLootTable =
    [
        new("Oak Log", "Wood", "Common", 1, 15),
        new("Birch Log", "Wood", "Common", 5, 25),
        new("Maple Log", "Wood", "Uncommon", 10, 40),
        new("Yew Log", "Wood", "Uncommon", 18, 60),
        new("Redwood Log", "Wood", "Rare", 25, 85),
        new("Ironwood Log", "Wood", "Epic", 35, 120),
        new("Elderwood Log", "Wood", "Legendary", 45, 175),
    ];

    public static readonly GatherDrop[] CropLootTable =
    [
        new("Wheat", "Crop", "Common", 1, 15),
        new("Corn", "Crop", "Common", 5, 25),
        new("Potato", "Crop", "Common", 8, 30),
        new("Tomato", "Crop", "Uncommon", 14, 45),
        new("Pumpkin", "Crop", "Uncommon", 22, 65),
        new("Starfruit", "Crop", "Rare", 32, 100),
        new("Dragonfruit", "Crop", "Epic", 42, 150),
    ];

    public static readonly GatherDrop[] HerbLootTable =
    [
        new("Sage", "Herb", "Common", 1, 15),
        new("Mint", "Herb", "Common", 5, 25),
        new("Lavender", "Herb", "Uncommon", 10, 40),
        new("Wolfsbane", "Herb", "Uncommon", 18, 60),
        new("Nightshade", "Herb", "Rare", 25, 85),
        new("Moonpetal", "Herb", "Epic", 35, 120),
        new("Starbloom", "Herb", "Legendary", 45, 175),
    ];

    public static readonly GatherDrop[] FishLootTable =
    [
        new("Sardine", "Fish", "Common", 1, 15),
        new("Trout", "Fish", "Common", 5, 25),
        new("Salmon", "Fish", "Uncommon", 10, 40),
        new("Swordfish", "Fish", "Uncommon", 18, 60),
        new("Giant Squid", "Fish", "Rare", 25, 85),
        new("Abyssal Eel", "Fish", "Epic", 35, 120),
        new("Leviathan Scale", "Fish", "Legendary", 45, 175),
    ];

    // ─── Crafting Recipe Data ───
    public record Ingredient(string ItemName, int Quantity);
    public record CraftRecipe(string Name, string Skill, int RequiredLevel, Ingredient[] Materials,
        string ResultItem, string ResultType, string ResultRarity, int XpReward);

    // --- Cooking Recipes (15) ---
    public static readonly CraftRecipe[] CookingRecipes =
    [
        new("Bread", "Cooking", 1, [new("Wheat", 3)],
            "Bread", "Food", "Common", 20),
        new("Corn Chowder", "Cooking", 4, [new("Corn", 3), new("Potato", 1)],
            "Corn Chowder", "Food", "Common", 35),
        new("Baked Potato", "Cooking", 6, [new("Potato", 2)],
            "Baked Potato", "Food", "Common", 30),
        new("Tomato Soup", "Cooking", 9, [new("Tomato", 3)],
            "Tomato Soup", "Food", "Common", 45),
        new("Fish Stew", "Cooking", 12, [new("Trout", 2), new("Potato", 2), new("Sage", 1)],
            "Fish Stew", "Food", "Uncommon", 65),
        new("Herb-Crusted Salmon", "Cooking", 16, [new("Salmon", 1), new("Lavender", 1), new("Mint", 2)],
            "Herb-Crusted Salmon", "Food", "Uncommon", 80),
        new("Pumpkin Pie", "Cooking", 20, [new("Pumpkin", 2), new("Wheat", 3)],
            "Pumpkin Pie", "Food", "Uncommon", 90),
        new("Swordfish Steak", "Cooking", 24, [new("Swordfish", 1), new("Sage", 2), new("Tomato", 2)],
            "Swordfish Steak", "Food", "Rare", 110),
        new("Starfruit Tart", "Cooking", 28, [new("Starfruit", 2), new("Wheat", 3)],
            "Starfruit Tart", "Food", "Rare", 130),
        new("Elven Feast Platter", "Cooking", 32, [new("Salmon", 2), new("Starfruit", 1), new("Lavender", 2), new("Wheat", 4)],
            "Elven Feast Platter", "Food", "Rare", 155),
        new("Dragonfruit Sorbet", "Cooking", 36, [new("Dragonfruit", 2), new("Mint", 3)],
            "Dragonfruit Sorbet", "Food", "Epic", 185),
        new("Abyssal Sashimi", "Cooking", 38, [new("Abyssal Eel", 1), new("Nightshade", 1), new("Sage", 2)],
            "Abyssal Sashimi", "Food", "Epic", 200),
        new("Phoenix Roast", "Cooking", 42, [new("Dragonfruit", 2), new("Starfruit", 2), new("Wolfsbane", 1)],
            "Phoenix Roast", "Food", "Epic", 230),
        new("Celestial Banquet", "Cooking", 46, [new("Leviathan Scale", 1), new("Starfruit", 3), new("Moonpetal", 2), new("Wheat", 5)],
            "Celestial Banquet", "Food", "Legendary", 280),
        new("Dragon Steak", "Cooking", 50, [new("Leviathan Scale", 2), new("Dragonfruit", 3), new("Starbloom", 1)],
            "Dragon Steak", "Food", "Legendary", 350),
    ];

    // --- Alchemy Recipes (15) ---
    public static readonly CraftRecipe[] AlchemyRecipes =
    [
        new("Minor Health Potion", "Alchemy", 1, [new("Sage", 2)],
            "Minor Health Potion", "Potion", "Common", 20),
        new("Minor Mana Potion", "Alchemy", 3, [new("Mint", 2)],
            "Minor Mana Potion", "Potion", "Common", 25),
        new("Antidote", "Alchemy", 6, [new("Sage", 2), new("Mint", 1)],
            "Antidote", "Potion", "Common", 35),
        new("Health Potion", "Alchemy", 10, [new("Sage", 3), new("Lavender", 1)],
            "Health Potion", "Potion", "Uncommon", 55),
        new("Mana Potion", "Alchemy", 13, [new("Mint", 3), new("Lavender", 1)],
            "Mana Potion", "Potion", "Uncommon", 60),
        new("XP Potion", "Alchemy", 16, [new("Lavender", 2), new("Wolfsbane", 1)],
            "XP Potion", "Potion", "Uncommon", 75),
        new("Luck Potion", "Alchemy", 20, [new("Wolfsbane", 2), new("Lavender", 2)],
            "Luck Potion", "Potion", "Uncommon", 90),
        new("Greater Health Potion", "Alchemy", 24, [new("Wolfsbane", 2), new("Sage", 4)],
            "Greater Health Potion", "Potion", "Rare", 110),
        new("Speed Potion", "Alchemy", 28, [new("Nightshade", 1), new("Mint", 3)],
            "Speed Potion", "Potion", "Rare", 130),
        new("Invisibility Potion", "Alchemy", 32, [new("Nightshade", 2), new("Moonpetal", 1)],
            "Invisibility Potion", "Potion", "Rare", 155),
        new("Strength Elixir", "Alchemy", 36, [new("Moonpetal", 2), new("Wolfsbane", 2)],
            "Strength Elixir", "Potion", "Epic", 185),
        new("Elixir of Wisdom", "Alchemy", 39, [new("Moonpetal", 2), new("Nightshade", 2), new("Lavender", 3)],
            "Elixir of Wisdom", "Potion", "Epic", 210),
        new("Phoenix Tears", "Alchemy", 42, [new("Starbloom", 1), new("Moonpetal", 2)],
            "Phoenix Tears", "Potion", "Epic", 240),
        new("Elixir of Immortality", "Alchemy", 46, [new("Starbloom", 2), new("Moonpetal", 3), new("Nightshade", 2)],
            "Elixir of Immortality", "Potion", "Legendary", 290),
        new("Philosopher's Draught", "Alchemy", 50, [new("Starbloom", 3), new("Moonpetal", 3), new("Wolfsbane", 3)],
            "Philosopher's Draught", "Potion", "Legendary", 360),
    ];

    // --- Blacksmithing Recipes (15) ---
    public static readonly CraftRecipe[] BlacksmithingRecipes =
    [
        new("Copper Dagger", "Blacksmithing", 1, [new("Copper Ore", 3)],
            "Copper Dagger", "Weapon", "Common", 20),
        new("Copper Shield", "Blacksmithing", 3, [new("Copper Ore", 4), new("Oak Log", 2)],
            "Copper Shield", "Armor", "Common", 30),
        new("Iron Sword", "Blacksmithing", 7, [new("Iron Ore", 4), new("Oak Log", 1)],
            "Iron Sword", "Weapon", "Common", 45),
        new("Iron Chainmail", "Blacksmithing", 10, [new("Iron Ore", 6)],
            "Iron Chainmail", "Armor", "Uncommon", 60),
        new("Silver Rapier", "Blacksmithing", 14, [new("Silver Ore", 3), new("Birch Log", 1)],
            "Silver Rapier", "Weapon", "Uncommon", 75),
        new("Steel Greatshield", "Blacksmithing", 18, [new("Iron Ore", 5), new("Silver Ore", 2), new("Maple Log", 2)],
            "Steel Greatshield", "Armor", "Uncommon", 90),
        new("Gold-Inlaid Axe", "Blacksmithing", 22, [new("Gold Ore", 2), new("Iron Ore", 4), new("Yew Log", 1)],
            "Gold-Inlaid Axe", "Weapon", "Rare", 110),
        new("Mithril Longsword", "Blacksmithing", 26, [new("Mithril Ore", 4), new("Redwood Log", 1)],
            "Mithril Longsword", "Weapon", "Rare", 135),
        new("Mithril Armor", "Blacksmithing", 30, [new("Mithril Ore", 6), new("Iron Ore", 3)],
            "Mithril Armor", "Armor", "Rare", 160),
        new("Adamantite Warhammer", "Blacksmithing", 34, [new("Adamantite Ore", 4), new("Ironwood Log", 2)],
            "Adamantite Warhammer", "Weapon", "Epic", 190),
        new("Adamantite Plate", "Blacksmithing", 37, [new("Adamantite Ore", 6), new("Mithril Ore", 2)],
            "Adamantite Plate", "Armor", "Epic", 215),
        new("Dragonslayer Blade", "Blacksmithing", 40, [new("Adamantite Ore", 5), new("Orichalcum Ore", 1), new("Elderwood Log", 1)],
            "Dragonslayer Blade", "Weapon", "Epic", 245),
        new("Orichalcum Greatsword", "Blacksmithing", 44, [new("Orichalcum Ore", 5), new("Elderwood Log", 2)],
            "Orichalcum Greatsword", "Weapon", "Legendary", 280),
        new("Celestial Aegis", "Blacksmithing", 47, [new("Orichalcum Ore", 6), new("Adamantite Ore", 3), new("Mithril Ore", 3)],
            "Celestial Aegis", "Armor", "Legendary", 320),
        new("Worldbreaker", "Blacksmithing", 50, [new("Orichalcum Ore", 8), new("Adamantite Ore", 4), new("Elderwood Log", 3)],
            "Worldbreaker", "Weapon", "Legendary", 380),
    ];

    // --- Enchanting Recipes (10) ---
    public static readonly CraftRecipe[] EnchantingRecipes =
    [
        new("Fire Enchantment", "Enchanting", 1, [new("Sage", 3), new("Copper Ore", 2)],
            "Fire Enchantment", "Enchantment", "Common", 30),
        new("Ice Enchantment", "Enchanting", 6, [new("Mint", 3), new("Silver Ore", 1)],
            "Ice Enchantment", "Enchantment", "Common", 45),
        new("Lightning Enchantment", "Enchanting", 12, [new("Lavender", 3), new("Gold Ore", 1)],
            "Lightning Enchantment", "Enchantment", "Uncommon", 70),
        new("Poison Enchantment", "Enchanting", 18, [new("Nightshade", 2), new("Wolfsbane", 2)],
            "Poison Enchantment", "Enchantment", "Uncommon", 95),
        new("Lifesteal Enchantment", "Enchanting", 24, [new("Moonpetal", 2), new("Nightshade", 1), new("Gold Ore", 2)],
            "Lifesteal Enchantment", "Enchantment", "Rare", 125),
        new("Holy Enchantment", "Enchanting", 30, [new("Moonpetal", 3), new("Starfruit", 2)],
            "Holy Enchantment", "Enchantment", "Rare", 155),
        new("Shadow Enchantment", "Enchanting", 36, [new("Nightshade", 3), new("Moonpetal", 2), new("Elderwood Log", 1)],
            "Shadow Enchantment", "Enchantment", "Epic", 190),
        new("Arcane Enchantment", "Enchanting", 40, [new("Starbloom", 1), new("Moonpetal", 3), new("Mithril Ore", 2)],
            "Arcane Enchantment", "Enchantment", "Epic", 230),
        new("Void Enchantment", "Enchanting", 45, [new("Starbloom", 2), new("Nightshade", 3), new("Orichalcum Ore", 1)],
            "Void Enchantment", "Enchantment", "Legendary", 275),
        new("Celestial Enchantment", "Enchanting", 50, [new("Starbloom", 3), new("Moonpetal", 3), new("Orichalcum Ore", 2)],
            "Celestial Enchantment", "Enchantment", "Legendary", 340),
    ];

    // --- Jewelcrafting Recipes (10) ---
    public static readonly CraftRecipe[] JewelcraftingRecipes =
    [
        new("Copper Ring", "Jewelcrafting", 1, [new("Copper Ore", 3)],
            "Copper Ring", "Jewelry", "Common", 25),
        new("Silver Pendant", "Jewelcrafting", 7, [new("Silver Ore", 2)],
            "Silver Pendant", "Jewelry", "Common", 45),
        new("Gold Ring", "Jewelcrafting", 13, [new("Gold Ore", 2), new("Silver Ore", 1)],
            "Gold Ring", "Jewelry", "Uncommon", 70),
        new("Emerald Necklace", "Jewelcrafting", 18, [new("Gold Ore", 2), new("Sage", 3), new("Lavender", 2)],
            "Emerald Necklace", "Jewelry", "Uncommon", 95),
        new("Ruby Amulet", "Jewelcrafting", 24, [new("Gold Ore", 3), new("Wolfsbane", 2), new("Iron Ore", 2)],
            "Ruby Amulet", "Jewelry", "Rare", 125),
        new("Sapphire Crown", "Jewelcrafting", 30, [new("Mithril Ore", 3), new("Gold Ore", 2), new("Nightshade", 2)],
            "Sapphire Crown", "Jewelry", "Rare", 160),
        new("Diamond Bracelet", "Jewelcrafting", 36, [new("Adamantite Ore", 2), new("Gold Ore", 3), new("Moonpetal", 2)],
            "Diamond Bracelet", "Jewelry", "Epic", 200),
        new("Starfire Circlet", "Jewelcrafting", 40, [new("Orichalcum Ore", 2), new("Starbloom", 1), new("Gold Ore", 3)],
            "Starfire Circlet", "Jewelry", "Epic", 240),
        new("Moonstone Tiara", "Jewelcrafting", 45, [new("Orichalcum Ore", 3), new("Moonpetal", 3), new("Adamantite Ore", 2)],
            "Moonstone Tiara", "Jewelry", "Legendary", 290),
        new("Crown of the Cosmos", "Jewelcrafting", 50, [new("Orichalcum Ore", 4), new("Starbloom", 3), new("Moonpetal", 3), new("Gold Ore", 4)],
            "Crown of the Cosmos", "Jewelry", "Legendary", 360),
    ];

    // ─── All recipes combined for lookup ───
    public static readonly CraftRecipe[] AllCraftRecipes =
        CookingRecipes
            .Concat(AlchemyRecipes)
            .Concat(BlacksmithingRecipes)
            .Concat(EnchantingRecipes)
            .Concat(JewelcraftingRecipes)
            .ToArray();

    // ─── Profile Management ───

    public async Task<GatheringProfile> GetOrCreateGatheringProfileAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var profile = await ctx.GetTable<GatheringProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (profile is not null)
            return profile;

        profile = new GatheringProfile
        {
            UserId = userId,
            GuildId = guildId,
        };
        ctx.Add(profile);
        await ctx.SaveChangesAsync();
        return profile;
    }

    public async Task<CraftingProfile> GetOrCreateCraftingProfileAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var profile = await ctx.GetTable<CraftingProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId);

        if (profile is not null)
            return profile;

        profile = new CraftingProfile
        {
            UserId = userId,
            GuildId = guildId,
        };
        ctx.Add(profile);
        await ctx.SaveChangesAsync();
        return profile;
    }

    // ─── Inventory Helpers ───

    public async Task<List<PlayerInventoryItem>> GetInventoryAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PlayerInventoryItem>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.Quantity > 0)
            .ToListAsyncLinqToDB();
    }

    public async Task AddToInventoryAsync(ulong userId, ulong guildId, string itemName, string itemType, string rarity, int quantity = 1)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<PlayerInventoryItem>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId && x.ItemName == itemName);

        if (existing is not null)
        {
            await ctx.GetTable<PlayerInventoryItem>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new PlayerInventoryItem
                {
                    Quantity = existing.Quantity + quantity,
                });
        }
        else
        {
            ctx.Add(new PlayerInventoryItem
            {
                UserId = userId,
                GuildId = guildId,
                ItemName = itemName,
                ItemType = itemType,
                Quantity = quantity,
                Rarity = rarity,
            });
            await ctx.SaveChangesAsync();
        }
    }

    public async Task<bool> RemoveFromInventoryAsync(ulong userId, ulong guildId, string itemName, int quantity = 1)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<PlayerInventoryItem>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId && x.ItemName == itemName);

        if (existing is null || existing.Quantity < quantity)
            return false;

        var remaining = existing.Quantity - quantity;
        if (remaining <= 0)
        {
            await ctx.GetTable<PlayerInventoryItem>()
                .Where(x => x.Id == existing.Id)
                .DeleteAsync();
        }
        else
        {
            await ctx.GetTable<PlayerInventoryItem>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new PlayerInventoryItem { Quantity = remaining });
        }
        return true;
    }

    public async Task<bool> HasItemsAsync(ulong userId, ulong guildId, string itemName, int quantity)
    {
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<PlayerInventoryItem>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.GuildId == guildId && x.ItemName == itemName);

        return existing is not null && existing.Quantity >= quantity;
    }

    // ─── Gathering Methods ───

    private GatherDrop PickDrop(GatherDrop[] table, int level)
    {
        // Filter to items the player can gather at their level
        var available = table.Where(d => d.MinLevel <= level).ToArray();
        if (available.Length == 0)
            return table[0];

        // Weight toward higher-tier items as level increases, but lower tiers still possible
        // Higher level items get more weight the closer the player's level is
        var weights = new double[available.Length];
        for (var i = 0; i < available.Length; i++)
        {
            var diff = level - available[i].MinLevel;
            weights[i] = 1.0 + diff * 0.3 + (i == available.Length - 1 ? 2.0 : 0.0);
        }

        var totalWeight = weights.Sum();
        var roll = _rng.NextDouble() * totalWeight;
        var cumulative = 0.0;
        for (var i = 0; i < available.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return available[i];
        }
        return available[^1];
    }

    private static (int newLevel, int newXp, bool leveledUp) AddXp(int currentLevel, int currentXp, int xpGain)
    {
        var newXp = currentXp + xpGain;
        var leveledUp = false;
        var newLevel = currentLevel;

        while (newLevel < MAX_LEVEL && newXp >= XpForLevel(newLevel))
        {
            newXp -= XpForLevel(newLevel);
            newLevel++;
            leveledUp = true;
        }

        if (newLevel >= MAX_LEVEL)
            newXp = 0;

        return (newLevel, newXp, leveledUp);
    }

    public async Task<(bool success, string error, GatherDrop drop, int quantity, int newLevel, bool leveledUp, TimeSpan? cooldownLeft)>
        MineAsync(ulong userId, ulong guildId)
    {
        var profile = await GetOrCreateGatheringProfileAsync(userId, guildId);

        if (profile.LastMinedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - profile.LastMinedAt.Value;
            if (elapsed.TotalSeconds < GATHER_COOLDOWN_SECONDS)
                return (false, "cooldown", null, 0, profile.MiningLevel, false, TimeSpan.FromSeconds(GATHER_COOLDOWN_SECONDS) - elapsed);
        }

        var drop = PickDrop(OreLootTable, profile.MiningLevel);
        var quantity = _rng.Next(1, 4);
        var (newLevel, newXp, leveledUp) = AddXp(profile.MiningLevel, profile.MiningXp, drop.XpReward);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GatheringProfile>()
            .Where(x => x.Id == profile.Id)
            .UpdateAsync(_ => new GatheringProfile
            {
                MiningLevel = newLevel,
                MiningXp = newXp,
                LastMinedAt = DateTime.UtcNow,
            });

        await AddToInventoryAsync(userId, guildId, drop.ItemName, drop.ItemType, drop.Rarity, quantity);
        return (true, null, drop, quantity, newLevel, leveledUp, null);
    }

    public async Task<(bool success, string error, GatherDrop drop, int quantity, int newLevel, bool leveledUp, TimeSpan? cooldownLeft)>
        ChopAsync(ulong userId, ulong guildId)
    {
        var profile = await GetOrCreateGatheringProfileAsync(userId, guildId);

        if (profile.LastChoppedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - profile.LastChoppedAt.Value;
            if (elapsed.TotalSeconds < GATHER_COOLDOWN_SECONDS)
                return (false, "cooldown", null, 0, profile.WoodcuttingLevel, false, TimeSpan.FromSeconds(GATHER_COOLDOWN_SECONDS) - elapsed);
        }

        var drop = PickDrop(WoodLootTable, profile.WoodcuttingLevel);
        var quantity = _rng.Next(1, 4);
        var (newLevel, newXp, leveledUp) = AddXp(profile.WoodcuttingLevel, profile.WoodcuttingXp, drop.XpReward);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GatheringProfile>()
            .Where(x => x.Id == profile.Id)
            .UpdateAsync(_ => new GatheringProfile
            {
                WoodcuttingLevel = newLevel,
                WoodcuttingXp = newXp,
                LastChoppedAt = DateTime.UtcNow,
            });

        await AddToInventoryAsync(userId, guildId, drop.ItemName, drop.ItemType, drop.Rarity, quantity);
        return (true, null, drop, quantity, newLevel, leveledUp, null);
    }

    public async Task<(bool success, string error, GatherDrop drop, int quantity, int newLevel, bool leveledUp, TimeSpan? cooldownLeft)>
        FarmAsync(ulong userId, ulong guildId)
    {
        var profile = await GetOrCreateGatheringProfileAsync(userId, guildId);

        if (profile.LastHarvestedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - profile.LastHarvestedAt.Value;
            if (elapsed.TotalSeconds < GATHER_COOLDOWN_SECONDS)
                return (false, "cooldown", null, 0, profile.FarmingLevel, false, TimeSpan.FromSeconds(GATHER_COOLDOWN_SECONDS) - elapsed);
        }

        var drop = PickDrop(CropLootTable, profile.FarmingLevel);
        var quantity = _rng.Next(1, 4);
        var (newLevel, newXp, leveledUp) = AddXp(profile.FarmingLevel, profile.FarmingXp, drop.XpReward);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GatheringProfile>()
            .Where(x => x.Id == profile.Id)
            .UpdateAsync(_ => new GatheringProfile
            {
                FarmingLevel = newLevel,
                FarmingXp = newXp,
                LastHarvestedAt = DateTime.UtcNow,
            });

        await AddToInventoryAsync(userId, guildId, drop.ItemName, drop.ItemType, drop.Rarity, quantity);
        return (true, null, drop, quantity, newLevel, leveledUp, null);
    }

    public async Task<(bool success, string error, GatherDrop drop, int quantity, int newLevel, bool leveledUp, TimeSpan? cooldownLeft)>
        GatherHerbsAsync(ulong userId, ulong guildId)
    {
        var profile = await GetOrCreateGatheringProfileAsync(userId, guildId);

        if (profile.LastGatheredAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - profile.LastGatheredAt.Value;
            if (elapsed.TotalSeconds < GATHER_COOLDOWN_SECONDS)
                return (false, "cooldown", null, 0, profile.HerbGatheringLevel, false, TimeSpan.FromSeconds(GATHER_COOLDOWN_SECONDS) - elapsed);
        }

        var drop = PickDrop(HerbLootTable, profile.HerbGatheringLevel);
        var quantity = _rng.Next(1, 4);
        var (newLevel, newXp, leveledUp) = AddXp(profile.HerbGatheringLevel, profile.HerbGatheringXp, drop.XpReward);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GatheringProfile>()
            .Where(x => x.Id == profile.Id)
            .UpdateAsync(_ => new GatheringProfile
            {
                HerbGatheringLevel = newLevel,
                HerbGatheringXp = newXp,
                LastGatheredAt = DateTime.UtcNow,
            });

        await AddToInventoryAsync(userId, guildId, drop.ItemName, drop.ItemType, drop.Rarity, quantity);
        return (true, null, drop, quantity, newLevel, leveledUp, null);
    }

    public async Task<(bool success, string error, GatherDrop drop, int quantity, int newLevel, bool leveledUp, TimeSpan? cooldownLeft)>
        FishGatherAsync(ulong userId, ulong guildId)
    {
        var profile = await GetOrCreateGatheringProfileAsync(userId, guildId);

        if (profile.LastFishedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - profile.LastFishedAt.Value;
            if (elapsed.TotalSeconds < GATHER_COOLDOWN_SECONDS)
                return (false, "cooldown", null, 0, profile.FishingSkillLevel, false, TimeSpan.FromSeconds(GATHER_COOLDOWN_SECONDS) - elapsed);
        }

        var drop = PickDrop(FishLootTable, profile.FishingSkillLevel);
        var quantity = _rng.Next(1, 3);
        var (newLevel, newXp, leveledUp) = AddXp(profile.FishingSkillLevel, profile.FishingSkillXp, drop.XpReward);

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GatheringProfile>()
            .Where(x => x.Id == profile.Id)
            .UpdateAsync(_ => new GatheringProfile
            {
                FishingSkillLevel = newLevel,
                FishingSkillXp = newXp,
                LastFishedAt = DateTime.UtcNow,
            });

        await AddToInventoryAsync(userId, guildId, drop.ItemName, drop.ItemType, drop.Rarity, quantity);
        return (true, null, drop, quantity, newLevel, leveledUp, null);
    }

    // ─── Crafting Methods ───

    public async Task<(bool success, string error, CraftRecipe recipe, bool leveledUp, int newLevel)>
        CraftItemAsync(ulong userId, ulong guildId, string recipeName, string skillFilter)
    {
        // Find the recipe
        var recipes = skillFilter switch
        {
            "Cooking" => CookingRecipes,
            "Alchemy" => AlchemyRecipes,
            "Blacksmithing" => BlacksmithingRecipes,
            "Enchanting" => EnchantingRecipes,
            "Jewelcrafting" => JewelcraftingRecipes,
            _ => AllCraftRecipes,
        };

        var recipe = recipes.FirstOrDefault(r =>
            r.Name.Equals(recipeName, StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
            return (false, "recipe_not_found", null, false, 0);

        // Check skill level
        var craftProfile = await GetOrCreateCraftingProfileAsync(userId, guildId);
        var (currentLevel, currentXp) = recipe.Skill switch
        {
            "Cooking" => (craftProfile.CookingLevel, craftProfile.CookingXp),
            "Alchemy" => (craftProfile.AlchemyLevel, craftProfile.AlchemyXp),
            "Blacksmithing" => (craftProfile.BlacksmithingLevel, craftProfile.BlacksmithingXp),
            "Enchanting" => (craftProfile.EnchantingLevel, craftProfile.EnchantingXp),
            "Jewelcrafting" => (craftProfile.JewelcraftingLevel, craftProfile.JewelcraftingXp),
            _ => (1, 0),
        };

        if (currentLevel < recipe.RequiredLevel)
            return (false, $"need_level_{recipe.RequiredLevel}", recipe, false, currentLevel);

        // Check materials
        foreach (var mat in recipe.Materials)
        {
            var has = await HasItemsAsync(userId, guildId, mat.ItemName, mat.Quantity);
            if (!has)
                return (false, $"missing_{mat.ItemName}", recipe, false, currentLevel);
        }

        // Consume materials
        foreach (var mat in recipe.Materials)
            await RemoveFromInventoryAsync(userId, guildId, mat.ItemName, mat.Quantity);

        // Add crafted item
        await AddToInventoryAsync(userId, guildId, recipe.ResultItem, recipe.ResultType, recipe.ResultRarity);

        // Add XP
        var (newLevel, newXp, leveledUp) = AddXp(currentLevel, currentXp, recipe.XpReward);

        await using var ctx = _db.GetDbContext();
        switch (recipe.Skill)
        {
            case "Cooking":
                await ctx.GetTable<CraftingProfile>()
                    .Where(x => x.Id == craftProfile.Id)
                    .UpdateAsync(_ => new CraftingProfile { CookingLevel = newLevel, CookingXp = newXp });
                break;
            case "Alchemy":
                await ctx.GetTable<CraftingProfile>()
                    .Where(x => x.Id == craftProfile.Id)
                    .UpdateAsync(_ => new CraftingProfile { AlchemyLevel = newLevel, AlchemyXp = newXp });
                break;
            case "Blacksmithing":
                await ctx.GetTable<CraftingProfile>()
                    .Where(x => x.Id == craftProfile.Id)
                    .UpdateAsync(_ => new CraftingProfile { BlacksmithingLevel = newLevel, BlacksmithingXp = newXp });
                break;
            case "Enchanting":
                await ctx.GetTable<CraftingProfile>()
                    .Where(x => x.Id == craftProfile.Id)
                    .UpdateAsync(_ => new CraftingProfile { EnchantingLevel = newLevel, EnchantingXp = newXp });
                break;
            case "Jewelcrafting":
                await ctx.GetTable<CraftingProfile>()
                    .Where(x => x.Id == craftProfile.Id)
                    .UpdateAsync(_ => new CraftingProfile { JewelcraftingLevel = newLevel, JewelcraftingXp = newXp });
                break;
        }

        return (true, null, recipe, leveledUp, newLevel);
    }

    // ─── Recipe Lookup ───

    public static CraftRecipe[] GetRecipesForSkill(string skill)
        => skill.ToLowerInvariant() switch
        {
            "cook" or "cooking" => CookingRecipes,
            "brew" or "alchemy" => AlchemyRecipes,
            "forge" or "blacksmithing" => BlacksmithingRecipes,
            "enchant" or "enchanting" => EnchantingRecipes,
            "jewelry" or "jewelcrafting" => JewelcraftingRecipes,
            _ => AllCraftRecipes,
        };
}
