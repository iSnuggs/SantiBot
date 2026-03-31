#nullable disable
namespace SantiBot.Db.Models;

/// <summary>
/// Tracks per-user seasonal event claims (egg hunts, advent calendar, etc.)
/// Prevents infinite currency exploits by enforcing daily limits.
/// </summary>
public class SeasonalClaim : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    /// <summary>
    /// "egghunt" or "advent"
    /// </summary>
    public string ClaimType { get; set; }

    /// <summary>
    /// For advent: the day number (1-25). For egg hunt: 0 (unused).
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// When the claim was made.
    /// </summary>
    public DateTime ClaimedAt { get; set; }
}
