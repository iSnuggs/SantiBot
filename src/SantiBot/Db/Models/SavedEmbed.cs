#nullable disable
namespace SantiBot.Db.Models;

public class SavedEmbed : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong CreatorId { get; set; }
    public string Name { get; set; }
    public string EmbedJson { get; set; }
}
