#nullable disable
namespace SantiBot.Db.Models;

public class ContentAgeGate : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong RequiredRoleId { get; set; }
}
