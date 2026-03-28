#nullable disable
namespace SantiBot.Db.Models;

public class UserBirthday : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

public class BirthdayConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong? AnnouncementChannelId { get; set; }
    public ulong? BirthdayRoleId { get; set; }
    public string Message { get; set; } = "Happy Birthday {0}! \ud83c\udf82";
}
