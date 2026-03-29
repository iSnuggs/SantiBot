#nullable disable
namespace SantiBot.Db.Models;

public class UserMood : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Emoji { get; set; }
    public string Message { get; set; }
    public DateTime ExpiresAt { get; set; }
}
