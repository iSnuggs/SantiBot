#nullable disable
namespace SantiBot.Db.Models;

public class VoiceXpConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public int XpPerMinute { get; set; }
}
