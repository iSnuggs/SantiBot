#nullable disable
namespace SantiBot.Db.Models;

public class FormModel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong CreatorId { get; set; }
    public string Title { get; set; }

    /// <summary>
    /// JSON array of question strings
    /// </summary>
    public string QuestionsJson { get; set; }

    /// <summary>
    /// Channel where responses are posted
    /// </summary>
    public ulong ResponseChannelId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FormResponse : DbEntity
{
    public int FormId { get; set; }
    public ulong UserId { get; set; }

    /// <summary>
    /// JSON array of answer strings
    /// </summary>
    public string AnswersJson { get; set; }

    public DateTime SubmittedAt { get; set; }
}
