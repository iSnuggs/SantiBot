#nullable disable
namespace SantiBot.Db.Models;

public class InviteTrackConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public ulong? LogChannelId { get; set; }
}
