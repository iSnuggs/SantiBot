#nullable disable
namespace SantiBot.Db.Models;

public class EmbedFixConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public bool DeleteOriginal { get; set; }
    public string EnabledPlatforms { get; set; } = "twitter,instagram,tiktok,reddit,bluesky";
}
