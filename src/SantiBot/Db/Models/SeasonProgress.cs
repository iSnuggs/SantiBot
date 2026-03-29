#nullable disable
namespace SantiBot.Db.Models;

public class SeasonProgress : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int SeasonNumber { get; set; }
    public long SeasonXp { get; set; }
    public int ClaimedLevel { get; set; }
}
