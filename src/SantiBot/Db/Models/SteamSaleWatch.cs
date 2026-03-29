#nullable disable
namespace SantiBot.Db.Models;

public class SteamSaleWatch : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string AppId { get; set; }
    public string GameName { get; set; }
    public bool LastOnSale { get; set; }
}
