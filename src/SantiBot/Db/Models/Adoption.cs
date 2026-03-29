#nullable disable
namespace SantiBot.Db.Models;

public class Adoption : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ParentId { get; set; }
    public ulong ChildId { get; set; }
}
