#nullable disable
namespace SantiBot.Db.Models;

public class BanSyncConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong LinkedGuildId { get; set; }
}

public class BanSyncEntry : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong BannedUserId { get; set; }
    public string Reason { get; set; }
    public ulong BannedByUserId { get; set; }
}
