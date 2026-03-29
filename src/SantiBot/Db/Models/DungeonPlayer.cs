#nullable disable
namespace SantiBot.Db.Models;

public class DungeonPlayer : DbEntity
{
    public ulong UserId { get; set; }
    public int DungeonsCleared { get; set; }
    public int MonstersKilled { get; set; }
    public long TotalLoot { get; set; }
}
