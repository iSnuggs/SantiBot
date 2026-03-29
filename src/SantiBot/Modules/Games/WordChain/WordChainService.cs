#nullable disable
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Games.WordChain;

public sealed class WordChainService : INService
{
    private readonly ICurrencyService _cs;
    public readonly ConcurrentDictionary<ulong, WordChainGame> ActiveGames = new();

    public WordChainService(ICurrencyService cs)
    {
        _cs = cs;
    }

    public class WordChainGame
    {
        public ulong ChannelId { get; set; }
        public List<ulong> Players { get; set; } = new();
        public List<ulong> EliminatedPlayers { get; set; } = new();
        public HashSet<string> UsedWords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string LastWord { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public DateTime TurnStarted { get; set; }
        public bool IsActive { get; set; }
    }

    public (bool Success, string Message) StartGame(ulong channelId, ulong userId)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A word chain game is already running!");

        var game = new WordChainGame
        {
            ChannelId = channelId,
            Players = new() { userId },
            IsActive = true,
            TurnStarted = DateTime.UtcNow
        };

        ActiveGames[channelId] = game;
        return (true, "🔤 **Word Chain started!** Type any word to begin. Next player's word must start with the last letter of the previous word. 30s per turn. Repeats = elimination!");
    }

    public (bool Success, string Message) JoinGame(ulong channelId, ulong userId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game running!");

        if (game.Players.Contains(userId))
            return (false, "Already playing!");

        game.Players.Add(userId);
        return (true, $"<@{userId}> joined! ({game.Players.Count} players)");
    }

    public async Task<(bool Valid, string Message, bool GameOver)> SubmitWordAsync(ulong channelId, ulong userId, string word)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game) || !game.IsActive)
            return (false, "", false);

        if (game.EliminatedPlayers.Contains(userId))
            return (false, "", false);

        if (!game.Players.Contains(userId))
            return (false, "", false);

        word = word.ToLower().Trim();

        // Check if it's this player's turn
        var activePlayers = game.Players.Where(p => !game.EliminatedPlayers.Contains(p)).ToList();
        if (activePlayers.Count == 0) return (false, "", false);

        var currentPlayer = activePlayers[game.CurrentPlayerIndex % activePlayers.Count];
        if (currentPlayer != userId)
            return (false, "", false);

        // Check rules
        if (game.UsedWords.Contains(word))
        {
            game.EliminatedPlayers.Add(userId);
            game.CurrentPlayerIndex++;
            var remaining = activePlayers.Count - 1;

            if (remaining <= 1)
            {
                var winner = activePlayers.First(p => !game.EliminatedPlayers.Contains(p));
                await _cs.AddAsync(winner, 200, new TxData("wordchain", "win"));
                ActiveGames.TryRemove(channelId, out _);
                return (false, $"<@{userId}> used a repeated word and is eliminated!\n🏆 <@{winner}> wins the Word Chain! +200 🥠", true);
            }

            return (false, $"<@{userId}> used a repeated word! Eliminated! ({remaining} players left)", false);
        }

        if (game.LastWord is not null)
        {
            var expectedStart = game.LastWord[^1];
            if (word[0] != expectedStart)
            {
                game.EliminatedPlayers.Add(userId);
                game.CurrentPlayerIndex++;
                var remaining = activePlayers.Count - 1;

                if (remaining <= 1)
                {
                    var winner = activePlayers.First(p => !game.EliminatedPlayers.Contains(p));
                    await _cs.AddAsync(winner, 200, new TxData("wordchain", "win"));
                    ActiveGames.TryRemove(channelId, out _);
                    return (false, $"<@{userId}> — word must start with '{expectedStart}'! Eliminated!\n🏆 <@{winner}> wins! +200 🥠", true);
                }

                return (false, $"<@{userId}> — word must start with '{expectedStart}'! Eliminated!", false);
            }
        }

        game.UsedWords.Add(word);
        game.LastWord = word;
        game.CurrentPlayerIndex++;
        game.TurnStarted = DateTime.UtcNow;

        var nextActive = game.Players.Where(p => !game.EliminatedPlayers.Contains(p)).ToList();
        var nextPlayer = nextActive[game.CurrentPlayerIndex % nextActive.Count];

        return (true, $"✅ **{word}** — Next: <@{nextPlayer}> (word must start with '**{word[^1]}**')", false);
    }

    public void StopGame(ulong channelId)
    {
        ActiveGames.TryRemove(channelId, out _);
    }
}
