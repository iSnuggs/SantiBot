#nullable disable
namespace SantiBot.Db.Models;

public class UserSocial : DbEntity
{
    public ulong UserId { get; set; }
    public string Platform { get; set; }
    public string Handle { get; set; }
}
