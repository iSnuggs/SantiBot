#nullable disable
namespace SantiBot.Db.Models;

public class DailyStreak : DbEntity
{
    public ulong UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime LastClaimUtc { get; set; }
}
