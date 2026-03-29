#nullable disable
namespace SantiBot.Db.Models;

public class Friendship : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong User1Id { get; set; }
    public ulong User2Id { get; set; }
    public bool Accepted { get; set; }
    public int InteractionCount { get; set; }
}
