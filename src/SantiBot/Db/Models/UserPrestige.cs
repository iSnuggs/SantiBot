#nullable disable
namespace SantiBot.Db.Models;

public class UserPrestige : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int PrestigeLevel { get; set; }
    public DateTime LastPrestigeDate { get; set; }
}
