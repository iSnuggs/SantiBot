#nullable disable
namespace SantiBot.Db.Models;

public class WelcomeQuizConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public ulong? VerifiedRoleId { get; set; }
    public string QuestionsJson { get; set; } = "[]"; // [{question, answers[], correctIndex}]
    public ulong? QuizChannelId { get; set; }
    public int RequiredCorrect { get; set; } = 1; // how many right answers needed
}
