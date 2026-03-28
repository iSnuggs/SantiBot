#nullable disable
namespace SantiBot.Db.Models;

public class QuarantineConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public ulong? QuarantineRoleId { get; set; }
    public int MinAccountAgeDays { get; set; } = 7;
    public bool QuarantineNoAvatar { get; set; } = true;
    public ulong? LogChannelId { get; set; }
}
