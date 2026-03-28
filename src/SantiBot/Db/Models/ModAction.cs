#nullable disable
namespace SantiBot.Db.Models;

public class ModAction : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ModeratorUserId { get; set; }
    public string ActionType { get; set; } = "";
    public ulong TargetUserId { get; set; }
    public DateTime ActionAt { get; set; }
}
