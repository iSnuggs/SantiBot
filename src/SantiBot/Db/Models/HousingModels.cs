#nullable disable
namespace SantiBot.Db.Models;

public class PlayerHouse : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string HouseName { get; set; } = "My House";
    public string HouseSize { get; set; } = "Cottage"; // Cottage, Cabin, House, Manor, Mansion, Castle
    public int Level { get; set; } = 1;
    public int RoomCount { get; set; } = 1;
    public int GardenSize { get; set; }
    public int TrophySlots { get; set; } = 1;
    public int GuestBookEntries { get; set; }
    public ulong LastVisitedBy { get; set; }
    public long PurchasePrice { get; set; }
    public string Style { get; set; } = "Medieval";
}

public class HouseRoom : DbEntity
{
    public int HouseId { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomType { get; set; } = "Bedroom"; // Bedroom, Kitchen, Workshop, Library, Trophy, Garden, Aquarium, Music, Storage, Greenhouse
    public int FurnitureCount { get; set; }
    public string Decorations { get; set; } = "[]"; // JSON list
}

public class HouseFurniture : DbEntity
{
    public int HouseId { get; set; }
    public string RoomName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string ItemType { get; set; } = "Table"; // Table, Chair, Bed, Shelf, Lamp, Rug, Painting, Trophy, Plant, Aquarium, Instrument
    public string Rarity { get; set; } = "Common"; // Common, Uncommon, Rare, Epic, Legendary
    public string BonusEffect { get; set; } = ""; // e.g. "+5% garden yield", "+10 trophy slots"
}

public class GuestBookEntry : DbEntity
{
    public int HouseId { get; set; }
    public ulong VisitorUserId { get; set; }
    public string VisitorName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}
