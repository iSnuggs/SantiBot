#nullable disable
namespace SantiBot.Db.Models;

public class ThreadArchiveConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int ArchiveAfterMinutes { get; set; } = 1440;
    public bool KeepAlive { get; set; }
}
