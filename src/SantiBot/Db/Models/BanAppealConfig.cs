#nullable disable
namespace SantiBot.Db.Models;

public class BanAppealConfig : DbEntity
{
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }
    public ulong? ReviewChannelId { get; set; }
    public string AppealMessage { get; set; } = "You have been banned. To appeal, use the link below.";
}
