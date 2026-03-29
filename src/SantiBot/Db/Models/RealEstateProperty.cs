#nullable disable
namespace SantiBot.Db.Models;

public class RealEstateProperty : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string PropertyType { get; set; } // Shack, House, Mansion, Castle, Skyscraper
    public int UpgradeLevel { get; set; } // 0-3
    public DateTime LastCollected { get; set; }
}
