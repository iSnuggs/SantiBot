#nullable disable
namespace SantiBot.Db.Models;

public class DashWebhook : DbEntity
{
    public ulong GuildId { get; set; }
    public string Url { get; set; }
    public string Event { get; set; } // config_changed, member_warned, etc.
    public bool IsEnabled { get; set; } = true;
}
