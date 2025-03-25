using System.ComponentModel.DataAnnotations;
using NadekoBot.Modules.Games.Quests;

namespace NadekoBot.Db.Models;

public class UserQuest
{
    [Key]
    public int Id { get; set; }

    public int QuestNumber { get; set; }
    public ulong UserId { get; set; }

    public QuestIds QuestId { get; set; }

    public int Progress { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime DateAssigned { get; set; }
}

