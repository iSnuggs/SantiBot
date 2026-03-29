#nullable disable
namespace SantiBot.Db.Models;

public class EconomySeason : DbEntity
{
    public ulong GuildId { get; set; }
    public int SeasonNumber { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
}

public class SeasonEarnings : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int SeasonNumber { get; set; }
    public long TotalEarned { get; set; }
}
