#nullable disable
namespace SantiBot.Db.Models;

public class CryptoHolding : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public string CoinName { get; set; }
    public double Amount { get; set; }
}

public class CryptoCoin : DbEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; }
    public long Price { get; set; }
    public DateTime LastUpdated { get; set; }
}
