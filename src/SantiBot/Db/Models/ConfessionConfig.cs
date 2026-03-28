#nullable disable
namespace SantiBot.Db.Models;

public class ConfessionConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public bool Enabled { get; set; }
    public int NextConfessionNumber { get; set; } = 1;
}
