#nullable disable
namespace SantiBot.Db.Models;

public class CryptoAlert : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
    public string CoinId { get; set; }
    public string Direction { get; set; } // "above" or "below"
    public decimal TargetPrice { get; set; }
    public bool Triggered { get; set; }
}
