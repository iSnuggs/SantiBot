#nullable disable
namespace SantiBot.Db.Models;

public class SocialStat : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long TotalMessages { get; set; }
    public long TotalReactions { get; set; }
    public long TotalVoiceMinutes { get; set; }
    public long HelpfulReactions { get; set; }
    public long WeeklyMessages { get; set; }
    public long WeeklyReactions { get; set; }
    public long WeeklyVoiceMinutes { get; set; }
    public DateTime WeekStart { get; set; }
}
