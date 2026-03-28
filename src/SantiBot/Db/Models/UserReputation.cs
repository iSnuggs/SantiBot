#nullable disable
namespace SantiBot.Db.Models;

public class UserReputation : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int RepCount { get; set; }
}
