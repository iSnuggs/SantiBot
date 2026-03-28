#nullable disable
namespace SantiBot.Db.Models;

public class ScheduledEvent : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong CreatorUserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public ulong? ChannelId { get; set; }
    public ulong? PingRoleId { get; set; }
    public string RsvpUserIds { get; set; } = "";
    public bool ReminderSent { get; set; }
}
