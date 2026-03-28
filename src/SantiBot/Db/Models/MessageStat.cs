#nullable disable
namespace SantiBot.Db.Models;

public class MessageStat : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong ChannelId { get; set; }
    public int MessageCount { get; set; }
    public DateTime Date { get; set; }
}
