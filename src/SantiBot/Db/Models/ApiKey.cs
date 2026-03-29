#nullable disable
namespace SantiBot.Db.Models;

public class ApiKey : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Key { get; set; }
    public bool IsRevoked { get; set; }
}
