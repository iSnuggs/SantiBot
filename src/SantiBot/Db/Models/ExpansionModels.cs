#nullable disable
namespace SantiBot.Db.Models;

public class SkillTree : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string Class { get; set; } = "Warrior";
    public int SkillPoints { get; set; }
    public int Skill1Level { get; set; }
    public int Skill2Level { get; set; }
    public int Skill3Level { get; set; }
    public int Skill4Level { get; set; }
    public int Skill5Level { get; set; }
}

public class PrestigeData : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public int PrestigeLevel { get; set; }
    public int PrestigeBonusPercent { get; set; }
    public DateTime LastPrestigeAt { get; set; }
}

public class DungeonModifier : DbEntity
{
    public ulong GuildId { get; set; }
    public string ModifierName { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public double AtkMult { get; set; } = 1.0;
    public double DefMult { get; set; } = 1.0;
    public double HpMult { get; set; } = 1.0;
    public double XpMult { get; set; } = 1.0;
    public double LootMult { get; set; } = 1.0;
}

public class Bounty : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong PostedBy { get; set; }
    public long Amount { get; set; }
    public string Reason { get; set; }
    public bool IsClaimed { get; set; }
    public ulong ClaimedBy { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime ClaimedAt { get; set; }
}

public class TreasureHunt : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string HiddenWord { get; set; }
    public long Reward { get; set; }
    public bool IsFound { get; set; }
    public ulong FoundBy { get; set; }
    public DateTime HiddenAt { get; set; }
}

public class MarriageExpansion : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public ulong PartnerId { get; set; }
    public DateTime Anniversary { get; set; }
    public long SharedCurrency { get; set; }
    public long SharedXp { get; set; }
    public int ChildCount { get; set; }
    public string FamilyName { get; set; }
}

public class Horoscope : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string ZodiacSign { get; set; }
    public DateTime LastReadingAt { get; set; }
}

public class GoalTracker : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string GoalName { get; set; }
    public string Description { get; set; }
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? Deadline { get; set; }
}

public class ServerNewspaper : DbEntity
{
    public ulong GuildId { get; set; }
    public int Edition { get; set; }
    public string Content { get; set; }
    public DateTime PublishedAt { get; set; }
    public ulong GeneratedBy { get; set; }
}
