namespace SantiBot.Db.Models;

public class DashboardAuditLog : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = ""; // e.g. "Updated starboard settings"
    public string Section { get; set; } = ""; // e.g. "starboard"
    public DateTime Timestamp { get; set; }
}
