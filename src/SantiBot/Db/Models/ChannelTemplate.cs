#nullable disable
namespace SantiBot.Db.Models;

public class ChannelTemplate : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string SettingsJson { get; set; } // JSON: topic, slowmode, nsfw, category, permissions
    public ulong CreatedByUserId { get; set; }
}
