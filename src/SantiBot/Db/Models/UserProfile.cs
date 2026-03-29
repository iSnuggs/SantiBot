#nullable disable
namespace SantiBot.Db.Models;

public class UserProfile : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string Bio { get; set; }
    public string Title { get; set; }
    public string Pronouns { get; set; }
    public string Timezone { get; set; }
    public long MessageCount { get; set; }
    public string BackgroundId { get; set; }
    public string BackgroundName { get; set; }
    public string BackgroundColor { get; set; }
}
