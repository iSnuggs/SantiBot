#nullable disable
namespace SantiBot.Db.Models;

public class HealthReportConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool AutoEnabled { get; set; }
    public DateTime? LastReportDate { get; set; }
}
