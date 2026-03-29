#nullable disable
namespace SantiBot.Db.Models;

public class ModTranslateConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public string TargetLanguage { get; set; }
    public bool IsEnabled { get; set; }
}
