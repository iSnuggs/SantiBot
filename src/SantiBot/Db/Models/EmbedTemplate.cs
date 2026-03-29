#nullable disable
namespace SantiBot.Db.Models;

public class EmbedTemplate : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; } // welcome, rules, announcement, etc.
    public string EmbedJson { get; set; }
}
