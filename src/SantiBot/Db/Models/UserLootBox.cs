#nullable disable
namespace SantiBot.Db.Models;

public class UserLootBox : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int CommonBoxes { get; set; }
    public int UncommonBoxes { get; set; }
    public int RareBoxes { get; set; }
    public int LegendaryBoxes { get; set; }
    public int MythicBoxes { get; set; }
    public int UnopenedBoxes { get; set; }
}
