#nullable disable
namespace SantiBot.Db.Models;

public class ModActionTemplate : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Reason { get; set; }
}
