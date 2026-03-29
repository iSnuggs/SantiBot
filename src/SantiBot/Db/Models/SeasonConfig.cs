#nullable disable
namespace SantiBot.Db.Models;

public class SeasonConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public int SeasonNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool Active { get; set; }
}
