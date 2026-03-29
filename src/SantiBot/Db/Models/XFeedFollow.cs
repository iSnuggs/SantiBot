#nullable disable
namespace SantiBot.Db.Models;

public class XFeedFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Handle { get; set; }
    public string LastItemId { get; set; }
}
