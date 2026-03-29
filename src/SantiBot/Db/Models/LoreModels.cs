#nullable disable
namespace SantiBot.Db.Models;

public class LoreEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public string Category { get; set; } // Monster, Item, Location, Boss, NPC, History
    public string EntryName { get; set; }
    public string Description { get; set; }
    public string Emoji { get; set; }
    public bool IsDiscovered { get; set; }
    public ulong DiscoveredBy { get; set; }
    public DateTime? DiscoveredAt { get; set; }
}

public class PlayerDiscovery : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public int LoreEntryId { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}

public class TreasureMap : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string MapName { get; set; }
    public string Clue1 { get; set; }
    public string Clue2 { get; set; }
    public string Clue3 { get; set; }
    public long RewardCurrency { get; set; }
    public long RewardXp { get; set; }
    public string RewardItemName { get; set; }
    public bool IsSolved { get; set; }
    public ulong SolvedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

public class WorldEvent : DbEntity
{
    public ulong GuildId { get; set; }
    public string EventName { get; set; }
    public string EventType { get; set; } // Invasion, Festival, Eclipse, Storm, Plague, Blessing
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; }
    public string BonusEffect { get; set; }
}
