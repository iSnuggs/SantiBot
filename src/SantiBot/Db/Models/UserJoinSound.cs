#nullable disable
namespace SantiBot.Db.Models;

public class UserJoinSound : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string SoundUrl { get; set; } = "";
}
