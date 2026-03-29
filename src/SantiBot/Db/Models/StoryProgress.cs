#nullable disable
namespace SantiBot.Db.Models;

public class StoryProgress : DbEntity
{
    public ulong UserId { get; set; }
    public string QuestId { get; set; }
    public int Chapter { get; set; }
    public string ChoicePath { get; set; } // e.g. "A,B,A,C"
    public bool IsComplete { get; set; }
    public long RewardsEarned { get; set; }
}
