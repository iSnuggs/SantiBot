#nullable disable
namespace SantiBot.Db.Models;

public class UserAchievement : DbEntity
{
    public ulong UserId { get; set; }
    public string AchievementId { get; set; } = ""; // e.g. "msg_1000", "xp_level_10"
    public DateTime UnlockedAt { get; set; }
}
