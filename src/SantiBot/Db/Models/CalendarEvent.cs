#nullable disable
namespace SantiBot.Db.Models;

public class CalendarEvent : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong CreatorId { get; set; }
    public DateTime EventDate { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int? ReminderMinutesBefore { get; set; }
    public bool ReminderSent { get; set; }
}
