#nullable disable
namespace SantiBot.Db.Models;

public class UserPet : DbEntity
{
    public ulong UserId { get; set; }
    public string Name { get; set; } = "";
    public string Species { get; set; } = "Cat";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int Hunger { get; set; } = 100;
    public int Happiness { get; set; } = 100;
    public DateTime LastFedUtc { get; set; }
    public DateTime LastPlayedUtc { get; set; }
    public long TotalEarned { get; set; }
}
