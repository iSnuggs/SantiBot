#nullable disable
namespace SantiBot.Db.Models;

public class GatheringProfile : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    // Mining
    public int MiningLevel { get; set; } = 1;
    public int MiningXp { get; set; }
    public DateTime? LastMinedAt { get; set; }

    // Woodcutting
    public int WoodcuttingLevel { get; set; } = 1;
    public int WoodcuttingXp { get; set; }
    public DateTime? LastChoppedAt { get; set; }

    // Farming
    public int FarmingLevel { get; set; } = 1;
    public int FarmingXp { get; set; }
    public DateTime? LastHarvestedAt { get; set; }

    // Fishing Skill (separate from the Fish module)
    public int FishingSkillLevel { get; set; } = 1;
    public int FishingSkillXp { get; set; }
    public DateTime? LastFishedAt { get; set; }

    // Herb Gathering
    public int HerbGatheringLevel { get; set; } = 1;
    public int HerbGatheringXp { get; set; }
    public DateTime? LastGatheredAt { get; set; }
}

public class CraftingProfile : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    // Cooking
    public int CookingLevel { get; set; } = 1;
    public int CookingXp { get; set; }

    // Alchemy
    public int AlchemyLevel { get; set; } = 1;
    public int AlchemyXp { get; set; }

    // Blacksmithing
    public int BlacksmithingLevel { get; set; } = 1;
    public int BlacksmithingXp { get; set; }

    // Enchanting
    public int EnchantingLevel { get; set; } = 1;
    public int EnchantingXp { get; set; }

    // Jewelcrafting
    public int JewelcraftingLevel { get; set; } = 1;
    public int JewelcraftingXp { get; set; }
}

public class PlayerInventoryItem : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string ItemName { get; set; } = "";
    public string ItemType { get; set; } = ""; // Ore, Wood, Crop, Herb, Fish, Food, Potion, Weapon, Armor, Gem, Jewelry, Enchantment
    public int Quantity { get; set; }
    public string Rarity { get; set; } = "Common"; // Common, Uncommon, Rare, Epic, Legendary
}
