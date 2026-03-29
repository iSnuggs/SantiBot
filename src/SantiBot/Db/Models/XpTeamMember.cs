#nullable disable
namespace SantiBot.Db.Models;

public class XpTeamMember : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int TeamId { get; set; }
}
