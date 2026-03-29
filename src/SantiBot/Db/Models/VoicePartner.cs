#nullable disable
namespace SantiBot.Db.Models;

public class VoicePartner : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong User1Id { get; set; }
    public ulong User2Id { get; set; }
    public long SharedMinutes { get; set; }
}
