#nullable disable
namespace SantiBot.Db.Models;

public class IdlePlayer : DbEntity
{
    public ulong UserId { get; set; }
    public long Resources { get; set; }
    public double ResourcesPerSecond { get; set; }
    public int ClickPower { get; set; }
    public int PrestigeLevel { get; set; }
    public double PrestigeMultiplier { get; set; }
    public DateTime LastCollected { get; set; }
    // Upgrades stored as comma-separated "name:level" pairs
    public string Upgrades { get; set; }
}
