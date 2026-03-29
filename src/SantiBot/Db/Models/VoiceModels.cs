#nullable disable
namespace SantiBot.Db.Models;

public class SoundboardSound : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public ulong AddedBy { get; set; }
    public int PlayCount { get; set; }
    public string Category { get; set; } = "General";
}

public class TempVoiceConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong CreateChannelId { get; set; }
    public ulong CategoryId { get; set; }
    public string DefaultName { get; set; } = "{user}'s Channel";
    public int DefaultLimit { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class TempVoiceChannel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong OwnerId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class VoiceSessionLog : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public int DurationMinutes { get; set; }
    public bool WasStreaming { get; set; }
    public bool WasMuted { get; set; }
    public bool WasDeafened { get; set; }
}

public class StreamAlert : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong AlertChannelId { get; set; }
    public ulong StreamerUserId { get; set; }
    public string Platform { get; set; } = "Discord";
    public string CustomMessage { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastAlertAt { get; set; }
}

public class ContentSchedule : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; }
    public string Platform { get; set; }
    public string Description { get; set; }
    public DateTime ScheduledAt { get; set; }
    public bool IsCompleted { get; set; }
}

public class ChannelPointsConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public string PointsName { get; set; } = "Channel Points";
    public string PointsEmoji { get; set; } = "🔷";
    public int PointsPerMessage { get; set; } = 1;
    public int PointsPerMinuteVoice { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;
}

public class UserChannelPoints : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public long Points { get; set; }
    public long TotalEarned { get; set; }
    public long TotalSpent { get; set; }
}

public class ChannelPointReward : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public long Cost { get; set; }
    public string RewardType { get; set; } // "Role", "CustomMessage", "Highlight", "VIP"
    public ulong RoleId { get; set; }
    public int MaxRedemptions { get; set; }
    public int CurrentRedemptions { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class Prediction : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong CreatedBy { get; set; }
    public string Question { get; set; }
    public string Option1 { get; set; }
    public string Option2 { get; set; }
    public long Option1Points { get; set; }
    public long Option2Points { get; set; }
    public int Option1Voters { get; set; }
    public int Option2Voters { get; set; }
    public string Status { get; set; } = "Open"; // Open, Locked, Resolved
    public int? WinningOption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PredictionBet : DbEntity
{
    public int PredictionId { get; set; }
    public ulong UserId { get; set; }
    public int ChosenOption { get; set; }
    public long PointsBet { get; set; }
}

public class FanArtSubmission : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public int Votes { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; }
}

public class FeedSubscription : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string FeedType { get; set; } // "YouTube", "Twitch", "Reddit", "RSS", "Twitter", "Steam", "Weather"
    public string FeedUrl { get; set; }
    public string FeedName { get; set; }
    public string LastItemId { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ulong AddedBy { get; set; }
}

public class UptimeMonitor : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong AlertChannelId { get; set; }
    public string Url { get; set; }
    public string Name { get; set; }
    public bool IsUp { get; set; } = true;
    public DateTime? LastDownAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public int CheckIntervalMinutes { get; set; } = 5;
    public int ConsecutiveFailures { get; set; }
}
