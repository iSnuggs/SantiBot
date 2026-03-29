#nullable disable
namespace SantiBot.Db.Models;

public class TimeCapsule : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Message { get; set; }
    public DateTime DeliverAt { get; set; }
    public bool Delivered { get; set; }
}
