#nullable disable
namespace SantiBot.Db.Models;

public class SantiScheduledPost : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string PostType { get; set; } // "DailyFact", "ServerSummary", "Motivation", "Custom"
    public string CustomPrompt { get; set; }
    public string CronSchedule { get; set; } // "0 9 * * *" = daily 9 AM
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastPostedAt { get; set; }
    public ulong CreatedBy { get; set; }
}
