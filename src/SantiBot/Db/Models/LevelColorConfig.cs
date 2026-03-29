#nullable disable
namespace SantiBot.Db.Models;

public class LevelColorConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public string StartColor { get; set; }
    public string EndColor { get; set; }
}
