#nullable disable
namespace SantiBot.Db.Models;

public class RaidBoss : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    // Boss identity
    public string Name { get; set; }
    public string Emoji { get; set; }

    // Stats
    public long MaxHp { get; set; }
    public long CurrentHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    // Phase tracking (4 phases: 100%, 75%, 50%, 25%)
    public int CurrentPhase { get; set; } = 1;

    // Reward pools
    public long XpReward { get; set; }
    public long CurrencyReward { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime SpawnedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DefeatedAt { get; set; }

    // How it spawned
    public bool WasRandomSpawn { get; set; }
}

public class RaidBossParticipant : DbEntity
{
    public int RaidBossId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    public long DamageDealt { get; set; }
    public int HitCount { get; set; }
    public DateTime LastAttackAt { get; set; } = DateTime.UtcNow;
}

public class RaidBossConfig : DbEntity
{
    public ulong GuildId { get; set; }

    // Random spawn settings
    public bool RandomSpawnsEnabled { get; set; } = true;
    public int MinDungeonClears { get; set; } = 5;
    public int MaxDungeonClears { get; set; } = 25;

    // Current counter toward next random spawn
    public int DungeonClearsSinceLastRaid { get; set; }
    public int NextSpawnThreshold { get; set; }

    // Channel to spawn random raids in
    public ulong SpawnChannelId { get; set; }
}
