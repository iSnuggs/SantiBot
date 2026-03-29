#nullable disable
namespace SantiBot.Db.Models;

public class ServerEvent : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong CreatedBy { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>
    /// "GameNight", "MovieNight", "Tournament", "StudySession", "Custom"
    /// </summary>
    public string EventType { get; set; } = "Custom";

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsRecurring { get; set; }

    /// <summary>
    /// "Daily", "Weekly", "Monthly", or null
    /// </summary>
    public string RecurrencePattern { get; set; }

    /// <summary>
    /// 0 means unlimited
    /// </summary>
    public int MaxAttendees { get; set; }

    /// <summary>
    /// "Upcoming", "Active", "Completed", "Cancelled"
    /// </summary>
    public string Status { get; set; } = "Upcoming";
}

public class EventRsvp : DbEntity
{
    public int EventId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }

    /// <summary>
    /// "Going", "Maybe", "NotGoing"
    /// </summary>
    public string Status { get; set; } = "Going";

    public DateTime RsvpAt { get; set; } = DateTime.UtcNow;
}

public class EventReminder : DbEntity
{
    public int EventId { get; set; }
    public ulong GuildId { get; set; }
    public int ReminderMinutesBefore { get; set; }
    public bool Sent { get; set; }
}

public class MovieNightPoll : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public bool IsActive { get; set; } = true;
    public ulong CreatedBy { get; set; }
}

public class MovieNightOption : DbEntity
{
    public int PollId { get; set; }
    public string MovieTitle { get; set; } = "";
    public ulong AddedBy { get; set; }
    public int Votes { get; set; }
}
