#nullable disable
namespace SantiBot.Db.Models;

public class CustomScript : DbEntity
{
    public ulong GuildId { get; set; }
    public string Trigger { get; set; }
    public string Script { get; set; }
    public bool IsEnabled { get; set; } = true;
}
