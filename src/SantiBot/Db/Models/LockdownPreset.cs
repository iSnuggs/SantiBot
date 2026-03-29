#nullable disable
namespace SantiBot.Db.Models;

public class LockdownPreset : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string PermissionsJson { get; set; } // JSON of channel permission overwrites
    public ulong CreatedByUserId { get; set; }
}
