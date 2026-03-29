#nullable disable
namespace SantiBot.Db.Models;

public class XpBooster : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; } // 0 = server-wide
    public double Multiplier { get; set; }
    public DateTime ExpiresAt { get; set; }
}
