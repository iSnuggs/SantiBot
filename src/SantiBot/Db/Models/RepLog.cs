#nullable disable
namespace SantiBot.Db.Models;

public class RepLog : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong GiverUserId { get; set; }
    public ulong ReceiverUserId { get; set; }
    public DateTime GivenAt { get; set; }
}
