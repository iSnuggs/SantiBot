#nullable disable
namespace SantiBot.Db.Models;

public class RaceCar : DbEntity
{
    public ulong UserId { get; set; }
    public int Speed { get; set; }
    public int Handling { get; set; }
    public int Nitro { get; set; }
    public int Wins { get; set; }
    public int Races { get; set; }
}
