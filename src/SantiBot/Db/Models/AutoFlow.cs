#nullable disable
namespace SantiBot.Db.Models;

public class AutoFlow : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string FlowJson { get; set; } // JSON trigger->response chain
    public bool IsEnabled { get; set; } = true;
}
