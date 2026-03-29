#nullable disable
namespace SantiBot.Db.Models;

public class WhiteLabelConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public string BotName { get; set; }
    public string AvatarUrl { get; set; }
    public string PrimaryColor { get; set; }
}
