#nullable disable
namespace SantiBot.Db.Models;

public class UserJob : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string JobName { get; set; } // Janitor, Cook, Developer, CEO
    public int TimesWorked { get; set; }
    public DateTime LastWorked { get; set; }
}
