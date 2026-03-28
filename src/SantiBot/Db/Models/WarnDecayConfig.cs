#nullable disable
namespace SantiBot.Db.Models;

public class WarnDecayConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public int DecayDays { get; set; } = 30;
    public int MinWarnsToDecay { get; set; } = 1;
}
