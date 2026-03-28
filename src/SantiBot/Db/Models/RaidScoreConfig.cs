#nullable disable
namespace SantiBot.Db.Models;

public class RaidScoreConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// Risk score threshold (0-100). Users scoring at or above this are actioned.
    /// </summary>
    public int ThresholdScore { get; set; } = 70;

    public ulong? AlertChannelId { get; set; }

    /// <summary>
    /// Action to take when a user exceeds the threshold: Alert, Quarantine, Kick, Ban
    /// </summary>
    public string ActionOnThreshold { get; set; } = "Alert";
}
