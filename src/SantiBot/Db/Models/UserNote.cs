#nullable disable
namespace SantiBot.Db.Models;

public class UserNote : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong ModeratorId { get; set; }
    public string Note { get; set; }
    public string ActionType { get; set; } // note, warn, mute, ban, kick
}
