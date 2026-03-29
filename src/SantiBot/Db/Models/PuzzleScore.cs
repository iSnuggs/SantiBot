#nullable disable
namespace SantiBot.Db.Models;

public class PuzzleScore : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int TotalSolved { get; set; }
    public int TotalPoints { get; set; }
    public DateTime LastSolvedDate { get; set; }
}
