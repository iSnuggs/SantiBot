#nullable disable
namespace SantiBot.Db.Models;

public class ScheduledRoleGrant : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public ulong RoleId { get; set; }
    public bool IsGrant { get; set; } = true; // true = add role, false = remove role
    public DateTime ScheduledFor { get; set; }
    public bool Executed { get; set; }
    public ulong ScheduledByUserId { get; set; }
}
