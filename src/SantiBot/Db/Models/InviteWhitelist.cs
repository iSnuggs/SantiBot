#nullable disable
namespace SantiBot.Db.Models;

public class InviteWhitelist : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong AllowedServerId { get; set; }
}

public class InviteWhitelistConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool IsEnabled { get; set; }
}
