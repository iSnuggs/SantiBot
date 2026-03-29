#nullable disable
namespace SantiBot.Db.Models;

public class ServerBackup : DbEntity
{
    public ulong GuildId { get; set; }
    public string BackupJson { get; set; } // Full server structure as JSON
    public ulong CreatedByUserId { get; set; }
    public string Description { get; set; }
}
