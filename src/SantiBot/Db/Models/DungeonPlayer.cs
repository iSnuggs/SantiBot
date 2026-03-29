#nullable disable
namespace SantiBot.Db.Models;

public class DungeonPlayer : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    // Level & XP
    public int Level { get; set; } = 1;
    public long Xp { get; set; }

    // Base stats (grow with level)
    public int BaseHp { get; set; } = 100;
    public int BaseAttack { get; set; } = 20;
    public int BaseDefense { get; set; } = 10;

    // Class & Race
    public string Class { get; set; } = "Adventurer";
    public string Race { get; set; } = "Human";

    // Lifetime stats
    public int DungeonsCleared { get; set; }
    public int MonstersKilled { get; set; }
    public long TotalLoot { get; set; }
    public int HighestDifficulty { get; set; }
    public int DeathCount { get; set; }

    // Equipped item IDs (0 = nothing)
    public int WeaponId { get; set; }
    public int ArmorId { get; set; }
    public int AccessoryId { get; set; }
}
