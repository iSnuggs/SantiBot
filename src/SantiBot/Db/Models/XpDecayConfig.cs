#nullable disable
namespace SantiBot.Db.Models;

public class XpDecayConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public int InactiveDays { get; set; }
    public long XpLostPerDay { get; set; }
}
