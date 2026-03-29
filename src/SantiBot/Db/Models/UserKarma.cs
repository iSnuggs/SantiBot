#nullable disable
namespace SantiBot.Db.Models;

public class UserKarma : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Upvotes { get; set; }
    public long Downvotes { get; set; }
}
