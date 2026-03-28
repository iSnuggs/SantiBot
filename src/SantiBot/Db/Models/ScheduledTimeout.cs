#nullable disable
namespace SantiBot.Db.Models;

public class ScheduledTimeout : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong TargetUserId { get; set; }
    public ulong ModeratorUserId { get; set; }
    public DateTime ScheduledFor { get; set; }
    public int DurationMinutes { get; set; }
    public string Reason { get; set; } = "";
    public bool Executed { get; set; }
}
