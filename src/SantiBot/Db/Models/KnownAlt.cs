#nullable disable
namespace SantiBot.Db.Models;

public class KnownAlt : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong MainUserId { get; set; }
    public ulong AltUserId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime DetectedAt { get; set; }
}
