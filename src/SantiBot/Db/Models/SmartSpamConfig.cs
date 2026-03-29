#nullable disable
namespace SantiBot.Db.Models;

public class SmartSpamConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool IsEnabled { get; set; }
    public int Threshold { get; set; } = 50;
    public string Action { get; set; } = "delete"; // delete, warn, mute
}
