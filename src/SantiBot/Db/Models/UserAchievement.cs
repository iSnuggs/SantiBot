#nullable disable
namespace SantiBot.Db.Models;

public class UserAchievement : DbEntity
{
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public string AchievementId { get; set; } = "";
    public string AchievementName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Emoji { get; set; } = "";
    public DateTime UnlockedAt { get; set; }
}
