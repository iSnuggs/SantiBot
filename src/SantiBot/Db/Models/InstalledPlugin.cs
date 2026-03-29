#nullable disable
namespace SantiBot.Db.Models;

public class InstalledPlugin : DbEntity
{
    public ulong GuildId { get; set; }
    public string PluginName { get; set; }
    public string Version { get; set; }
    public bool IsEnabled { get; set; } = true;
}
