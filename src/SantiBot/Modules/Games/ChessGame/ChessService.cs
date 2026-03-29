#nullable disable
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.ChessGame;

public sealed class ChessService : INService
{
    private readonly ICurrencyService _cs;
    public readonly ConcurrentDictionary<ulong, ChessMatch> ActiveGames = new();

    public ChessService(ICurrencyService cs)
    {
        _cs = cs;
    }

    public class ChessMatch
    {
        public ulong ChannelId { get; set; }
        public ulong WhiteId { get; set; }
        public string WhiteName { get; set; }
        public ulong BlackId { get; set; }
        public string BlackName { get; set; }
        public char[,] Board { get; set; }
        public bool IsWhiteTurn { get; set; } = true;
        public int MoveCount { get; set; }
    }

    // Standard chess starting position
    private char[,] CreateBoard()
    {
        var board = new char[8, 8];
        // Row 0 = rank 8 (top, black side), Row 7 = rank 1 (bottom, white side)
        var backRow = new[] { 'R', 'N', 'B', 'Q', 'K', 'B', 'N', 'R' };

        for (int i = 0; i < 8; i++)
        {
            board[0, i] = char.ToLower(backRow[i]); // Black pieces (lowercase)
            board[1, i] = 'p'; // Black pawns
            board[6, i] = 'P'; // White pawns
            board[7, i] = backRow[i]; // White pieces (uppercase)

            for (int j = 2; j < 6; j++)
                board[j, i] = '.';
        }

        return board;
    }

    public (bool Success, string Message) Challenge(ulong channelId, ulong whiteId, string whiteName, ulong blackId, string blackName)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A chess game is already running in this channel!");

        if (whiteId == blackId)
            return (false, "You can't play against yourself!");

        var match = new ChessMatch
        {
            ChannelId = channelId,
            WhiteId = whiteId,
            WhiteName = whiteName,
            BlackId = blackId,
            BlackName = blackName,
            Board = CreateBoard()
        };

        ActiveGames[channelId] = match;
        return (true, $"♟️ **Chess Match!** {whiteName} (White) vs {blackName} (Black)\n{RenderBoard(match.Board)}\n{whiteName}'s turn! Use `.chess move e2 e4` (from to).");
    }

    public async Task<(bool Success, string Message)> MoveAsync(ulong channelId, ulong userId, string from, string to)
    {
        if (!ActiveGames.TryGetValue(channelId, out var match))
            return (false, "No chess game!");

        var isWhite = userId == match.WhiteId;
        var isBlack = userId == match.BlackId;

        if (!isWhite && !isBlack)
            return (false, "You're not in this game!");

        if ((isWhite && !match.IsWhiteTurn) || (isBlack && match.IsWhiteTurn))
            return (false, "Not your turn!");

        // Parse coordinates
        if (!TryParseSquare(from, out var fromRow, out var fromCol) ||
            !TryParseSquare(to, out var toRow, out var toCol))
            return (false, "Invalid square! Use notation like 'e2 e4'.");

        var piece = match.Board[fromRow, fromCol];
        if (piece == '.')
            return (false, "No piece at that square!");

        // Check piece ownership (uppercase = white, lowercase = black)
        if (isWhite && char.IsLower(piece))
            return (false, "That's not your piece!");
        if (isBlack && char.IsUpper(piece))
            return (false, "That's not your piece!");

        // Check if capturing own piece
        var target = match.Board[toRow, toCol];
        if (target != '.' && ((isWhite && char.IsUpper(target)) || (isBlack && char.IsLower(target))))
            return (false, "Can't capture your own piece!");

        // Simplified move validation (allows any move that doesn't capture own piece)
        var captured = target != '.' ? $" (captured {GetPieceName(target)})" : "";

        // Check if capturing king (game over)
        var isKingCapture = char.ToUpper(target) == 'K';

        match.Board[toRow, toCol] = piece;
        match.Board[fromRow, fromCol] = '.';
        match.IsWhiteTurn = !match.IsWhiteTurn;
        match.MoveCount++;

        if (isKingCapture)
        {
            var winner = isWhite ? match.WhiteName : match.BlackName;
            var winnerId = isWhite ? match.WhiteId : match.BlackId;
            await _cs.AddAsync(winnerId, 200, new TxData("chess", "win"));
            ActiveGames.TryRemove(channelId, out _);
            return (true, $"♟️ **{from} -> {to}**{captured}\n{RenderBoard(match.Board)}\n\n🏆 **Checkmate! {winner} wins!** +200 🥠");
        }

        var nextPlayer = match.IsWhiteTurn ? match.WhiteName : match.BlackName;
        return (true, $"♟️ **{from} -> {to}**{captured}\n{RenderBoard(match.Board)}\n{nextPlayer}'s turn! (Move {match.MoveCount})");
    }

    public async Task<(bool Success, string Message)> ResignAsync(ulong channelId, ulong userId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var match))
            return (false, "No chess game!");

        var isWhite = userId == match.WhiteId;
        var winnerId = isWhite ? match.BlackId : match.WhiteId;
        var winnerName = isWhite ? match.BlackName : match.WhiteName;
        var loserName = isWhite ? match.WhiteName : match.BlackName;

        await _cs.AddAsync(winnerId, 200, new TxData("chess", "win"));
        ActiveGames.TryRemove(channelId, out _);

        return (true, $"🏳️ {loserName} resigns! **{winnerName} wins!** +200 🥠");
    }

    public string GetBoard(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var match))
            return null;
        return RenderBoard(match.Board);
    }

    private string RenderBoard(char[,] board)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```");
        sb.AppendLine("  a b c d e f g h");

        for (int row = 0; row < 8; row++)
        {
            sb.Append($"{8 - row} ");
            for (int col = 0; col < 8; col++)
            {
                var piece = board[row, col];
                var display = piece switch
                {
                    'K' => "♔", 'Q' => "♕", 'R' => "♖", 'B' => "♗", 'N' => "♘", 'P' => "♙",
                    'k' => "♚", 'q' => "♛", 'r' => "♜", 'b' => "♝", 'n' => "♞", 'p' => "♟",
                    _ => "· "
                };
                sb.Append(display);
                if (piece != '.') sb.Append(' ');
            }
            sb.AppendLine($" {8 - row}");
        }

        sb.AppendLine("  a b c d e f g h");
        sb.AppendLine("```");
        return sb.ToString();
    }

    private bool TryParseSquare(string square, out int row, out int col)
    {
        row = col = -1;
        if (square.Length != 2) return false;

        col = square[0] - 'a';
        row = 8 - (square[1] - '0');

        return col is >= 0 and < 8 && row is >= 0 and < 8;
    }

    private string GetPieceName(char piece) => char.ToUpper(piece) switch
    {
        'K' => "King",
        'Q' => "Queen",
        'R' => "Rook",
        'B' => "Bishop",
        'N' => "Knight",
        'P' => "Pawn",
        _ => "piece"
    };
}
