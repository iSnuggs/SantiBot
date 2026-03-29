#nullable disable
namespace SantiBot.Db.Models;

public class ScheduledTask : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Message { get; set; }
    public string CronExpression { get; set; }
    public DateTime? NextRun { get; set; }
    public bool IsEnabled { get; set; } = true;
}
