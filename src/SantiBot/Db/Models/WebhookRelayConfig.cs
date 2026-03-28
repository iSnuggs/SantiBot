#nullable disable
namespace SantiBot.Db.Models;

public class WebhookRelayConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public string EndpointId { get; set; } = "";
    public ulong TargetChannelId { get; set; }
    public string SecretKey { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string FilterField { get; set; } = "";
}
