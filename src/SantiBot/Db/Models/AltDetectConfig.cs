#nullable disable
namespace SantiBot.Db.Models;

public class AltDetectConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>
    /// Minimum account age in days. Accounts younger than this are flagged.
    /// </summary>
    public int MinAccountAgeDays { get; set; } = 3;

    /// <summary>
    /// Action on alt detection: Alert, Kick, Ban
    /// </summary>
    public string Action { get; set; } = "Alert";

    public ulong? AlertChannelId { get; set; }
}
