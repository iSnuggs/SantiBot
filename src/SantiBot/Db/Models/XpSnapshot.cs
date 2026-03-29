#nullable disable
namespace SantiBot.Db.Models;

public class XpSnapshot : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Xp { get; set; }
    public int Rank { get; set; }
    public DateTime SnapshotDate { get; set; }
}
