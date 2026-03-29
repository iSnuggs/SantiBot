#nullable disable
namespace SantiBot.Db.Models;

public class UserBadge : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string BadgeId { get; set; }
    public string BadgeName { get; set; }
    public string Emoji { get; set; }
    public string Category { get; set; }
    public string Rarity { get; set; } = "Common";
    public bool IsDisplayed { get; set; } // shown on profile
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

public class UserTitle : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string TitleId { get; set; }
    public string TitleName { get; set; }
    public string Color { get; set; } // hex color for display
    public bool IsActive { get; set; } // currently equipped
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

public class BattlePassProgress : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public int Season { get; set; }
    public int CurrentTier { get; set; } = 1;
    public long SeasonXp { get; set; }
    public bool IsPremium { get; set; }
    public int DailyChallengesCompleted { get; set; }
    public int WeeklyChallengesCompleted { get; set; }
    public DateTime LastDailyChallengeAt { get; set; }
}

public class BattlePassConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public int CurrentSeason { get; set; } = 1;
    public string SeasonName { get; set; } = "Season 1";
    public int MaxTier { get; set; } = 50;
    public long XpPerTier { get; set; } = 1000;
    public bool IsActive { get; set; } = true;
    public DateTime SeasonStartedAt { get; set; } = DateTime.UtcNow;
    public DateTime SeasonEndsAt { get; set; } = DateTime.UtcNow.AddDays(90);
}

public class DailyChallenge : DbEntity
{
    public ulong GuildId { get; set; }
    public string ChallengeId { get; set; }
    public string Description { get; set; }
    public long XpReward { get; set; }
    public DateTime ActiveDate { get; set; }
}
