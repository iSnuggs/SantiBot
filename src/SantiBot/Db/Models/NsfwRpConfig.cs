#nullable disable
namespace SantiBot.Db.Models;

public class NsfwRpConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
}
