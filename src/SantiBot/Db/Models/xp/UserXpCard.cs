#nullable disable
namespace SantiBot.Db.Models;

public class UserXpCard : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    /// <summary>
    /// Custom background URL for the rank card. Overrides shop background.
    /// </summary>
    public string BackgroundUrl { get; set; }

    /// <summary>
    /// Hex color for XP bar accent (e.g. "#FF5500"). Null = use default.
    /// </summary>
    public string AccentColor { get; set; }
}
