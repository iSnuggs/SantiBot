#nullable disable
namespace SantiBot.Db.Models;

public class RegexAutomodRule : DbEntity
{
    public ulong GuildId { get; set; }
    public string Pattern { get; set; }
    public RegexAutomodAction Action { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ulong AddedByUserId { get; set; }
}

public enum RegexAutomodAction
{
    Delete = 0,
    Warn = 1,
    Mute = 2,
    Ban = 3
}
