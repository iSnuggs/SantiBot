#nullable disable
namespace SantiBot.Db.Models;

public class DungeonItem : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    public string Name { get; set; }

    // Weapon, Armor, Accessory, Consumable
    public string Slot { get; set; }

    // Rarity: Common, Uncommon, Rare, Epic, Legendary
    public string Rarity { get; set; } = "Common";

    // Stat bonuses
    public int BonusHp { get; set; }
    public int BonusAttack { get; set; }
    public int BonusDefense { get; set; }

    // Special effect description (e.g. "Lifesteal 10%", "Crit +15%")
    public string SpecialEffect { get; set; }

    public bool IsEquipped { get; set; }
}
