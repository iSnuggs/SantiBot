#nullable disable
namespace SantiBot.Db.Models;

public class XpTeam : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public ulong OwnerId { get; set; }
    public long TotalXp { get; set; }
}
