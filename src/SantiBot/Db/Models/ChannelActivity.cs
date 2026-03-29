#nullable disable
namespace SantiBot.Db.Models;

public class ChannelActivity : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int MessageCount { get; set; }
    public DateTime TrackedDate { get; set; } // Date only (day granularity)
}
