#nullable disable
namespace SantiBot.Db.Models;

public class BotPlugin : DbEntity
{
    public ulong GuildId { get; set; }
    public string PluginName { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
}

public class WebhookEndpoint : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Secret { get; set; }
    public string TargetChannelId { get; set; }
    public string EventType { get; set; } // "message", "embed", "action"
    public int TriggerCount { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FeatureFlag : DbEntity
{
    public ulong GuildId { get; set; }
    public string FeatureName { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string EnabledForRoles { get; set; } // comma-separated role IDs
    public int RolloutPercent { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CommandLog : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong ChannelId { get; set; }
    public string CommandName { get; set; }
    public string Arguments { get; set; }
    public bool Success { get; set; }
    public int ExecutionMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class XpMultiplier : DbEntity
{
    public ulong GuildId { get; set; }
    public string Type { get; set; } // "Channel", "Role", "Global", "Event"
    public ulong TargetId { get; set; } // channelId or roleId, 0 for global
    public double Multiplier { get; set; } = 1.0;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class XpChallengeEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public string ChallengeName { get; set; }
    public string Description { get; set; }
    public string Requirement { get; set; } // "messages:100", "voice:60", "reactions:50"
    public long XpReward { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class XpChallengeParticipant : DbEntity
{
    public int ChallengeId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public int Progress { get; set; }
    public bool IsComplete { get; set; }
}
