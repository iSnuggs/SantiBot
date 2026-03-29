#nullable disable
namespace SantiBot.Db.Models;

public class WarningPoint : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong ModeratorId { get; set; }
    public string Reason { get; set; }
    public int Points { get; set; }
    public string Severity { get; set; } // minor, major, custom
}

public class WarningPointConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public int Threshold { get; set; }
    public string Action { get; set; } // warn, mute, kick, ban
}
