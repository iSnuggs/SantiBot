#nullable disable
namespace SantiBot.Db.Models;

public class Marriage : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong User1Id { get; set; }
    public ulong User2Id { get; set; }
}
