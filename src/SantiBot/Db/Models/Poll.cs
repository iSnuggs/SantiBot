#nullable disable
namespace SantiBot.Db.Models;

public class PollModel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong CreatorId { get; set; }
    public string Question { get; set; }

    /// <summary>
    /// JSON array of option strings, e.g. ["Option A","Option B","Option C"]
    /// </summary>
    public string OptionsJson { get; set; }

    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PollVote : DbEntity
{
    public int PollId { get; set; }
    public ulong UserId { get; set; }
    public int OptionIndex { get; set; }
}

public class SuggestionModel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong AuthorId { get; set; }
    public string Content { get; set; }
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;
    public string StatusReason { get; set; }
}

public enum SuggestionStatus
{
    Pending,
    Approved,
    Denied,
    Implemented
}
