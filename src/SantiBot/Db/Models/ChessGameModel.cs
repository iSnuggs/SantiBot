#nullable disable
namespace SantiBot.Db.Models;

public class ChessGameModel : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong WhitePlayerId { get; set; }
    public ulong BlackPlayerId { get; set; }
    public int WhiteWins { get; set; }
    public int BlackWins { get; set; }
    public int Draws { get; set; }
}
