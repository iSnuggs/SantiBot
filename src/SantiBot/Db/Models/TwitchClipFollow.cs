#nullable disable
namespace SantiBot.Db.Models;

public class TwitchClipFollow : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string TwitchChannel { get; set; }
    public string LastClipId { get; set; }
}
