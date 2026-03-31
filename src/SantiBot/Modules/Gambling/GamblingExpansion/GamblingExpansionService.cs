#nullable disable
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Gambling.GamblingExpansion;

public sealed class GamblingExpansionService : INService
{
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    // Active poker games per channel
    public readonly ConcurrentDictionary<ulong, PokerGame> ActivePokerGames = new();

    public GamblingExpansionService(ICurrencyService cs)
    {
        _cs = cs;
    }

    #region Coinflip
    public async Task<(bool Won, string Result, long Payout)> CoinflipAsync(ulong userId, long bet, string guess)
    {
        var removed = await _cs.RemoveAsync(userId, bet, new TxData("coinflip", "bet"));
        if (!removed)
            return (false, "Not enough 🥠!", 0);

        var isHeads = _rng.Next(2) == 0;
        var result = isHeads ? "Heads" : "Tails";
        var guessedHeads = guess.Equals("heads", StringComparison.OrdinalIgnoreCase) || guess.Equals("h", StringComparison.OrdinalIgnoreCase);
        var won = (isHeads && guessedHeads) || (!isHeads && !guessedHeads);

        if (won)
        {
            var payout = bet * 2;
            await _cs.AddAsync(userId, payout, new TxData("coinflip", "win"));
            return (true, result, payout);
        }

        return (false, result, 0);
    }
    #endregion

    #region Roulette
    public async Task<(bool Won, string Result, long Payout)> RouletteAsync(ulong userId, long bet, string guess)
    {
        var removed = await _cs.RemoveAsync(userId, bet, new TxData("roulette", "bet"));
        if (!removed)
            return (false, "Not enough 🥠!", 0);

        var number = _rng.Next(0, 37); // 0-36
        var isRed = new[] { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 }.Contains(number);
        var color = number == 0 ? "Green (0)" : (isRed ? "Red" : "Black");
        var result = $"{number} ({color})";

        bool won = false;
        long payout = 0;

        if (int.TryParse(guess, out var guessNum) && guessNum == number)
        {
            won = true;
            payout = bet * 36;
        }
        else if (guess.Equals("red", StringComparison.OrdinalIgnoreCase) && isRed)
        {
            won = true;
            payout = bet * 2;
        }
        else if (guess.Equals("black", StringComparison.OrdinalIgnoreCase) && !isRed && number != 0)
        {
            won = true;
            payout = bet * 2;
        }

        if (won)
            await _cs.AddAsync(userId, payout, new TxData("roulette", "win"));

        return (won, result, payout);
    }
    #endregion

    #region Blackjack Simple
    public async Task<(bool Won, string Result, long Payout)> BlackjackAsync(ulong userId, long bet)
    {
        var removed = await _cs.RemoveAsync(userId, bet, new TxData("blackjack", "bet"));
        if (!removed)
            return (false, "Not enough 🥠!", 0);

        // Simplified: deal hands
        var playerHand = DealHand();
        var dealerHand = DealHand();

        // Player auto-hits if under 17
        while (HandValue(playerHand) < 17 && HandValue(playerHand) > 0)
            playerHand.Add(DrawCard());

        // Dealer hits until 17+
        while (HandValue(dealerHand) < 17)
            dealerHand.Add(DrawCard());

        var playerVal = HandValue(playerHand);
        var dealerVal = HandValue(dealerHand);

        var playerCards = string.Join(" ", playerHand);
        var dealerCards = string.Join(" ", dealerHand);

        string result = $"Your hand: {playerCards} ({playerVal})\nDealer: {dealerCards} ({dealerVal})";

        if (playerVal > 21)
        {
            return (false, result + "\n**Bust! You lose.**", 0);
        }
        else if (dealerVal > 21 || playerVal > dealerVal)
        {
            var payout = playerVal == 21 && playerHand.Count == 2 ? (long)(bet * 2.5) : bet * 2;
            await _cs.AddAsync(userId, payout, new TxData("blackjack", "win"));
            return (true, result + $"\n**You win {payout} 🥠!**", payout);
        }
        else if (playerVal == dealerVal)
        {
            await _cs.AddAsync(userId, bet, new TxData("blackjack", "push"));
            return (false, result + "\n**Push! Bet returned.**", bet);
        }
        else
        {
            return (false, result + "\n**Dealer wins!**", 0);
        }
    }

    private List<string> DealHand()
    {
        return [DrawCard(), DrawCard()];
    }

    private static readonly string[] CardValues = ["A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"];
    private static readonly string[] Suits = ["♠", "♥", "♦", "♣"];

    private string DrawCard()
    {
        return CardValues[_rng.Next(CardValues.Length)] + Suits[_rng.Next(Suits.Length)];
    }

    /// <summary>
    /// Evaluate a 5-card poker hand. Returns a score where higher = better.
    /// Hand ranks: Royal Flush(9) > Straight Flush(8) > Four of a Kind(7) >
    /// Full House(6) > Flush(5) > Straight(4) > Three of a Kind(3) >
    /// Two Pair(2) > Pair(1) > High Card(0)
    /// Score = handRank * 1_000_000 + highCardTiebreaker
    /// </summary>
    private static int EvaluatePokerHand(List<string> hand)
    {
        // Parse cards into rank values and suits
        var ranks = new List<int>();
        var suits = new List<char>();

        foreach (var card in hand)
        {
            var suit = card[^1];
            var rankStr = card[..^1];
            var rank = rankStr switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                _ => int.TryParse(rankStr, out var r) ? r : 0
            };
            ranks.Add(rank);
            suits.Add(suit);
        }

        ranks.Sort();
        var isFlush = suits.All(s => s == suits[0]);
        var isStraight = ranks.Distinct().Count() == 5
            && (ranks[4] - ranks[0] == 4
                || (ranks.SequenceEqual(new[] { 2, 3, 4, 5, 14 }))); // Ace-low straight

        var groups = ranks.GroupBy(r => r).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        var highCard = ranks.OrderByDescending(r => r).Aggregate(0, (acc, r) => acc * 15 + r);

        // Royal Flush
        if (isFlush && isStraight && ranks.Contains(14) && ranks.Contains(13))
            return 9_000_000 + highCard;

        // Straight Flush
        if (isFlush && isStraight)
            return 8_000_000 + highCard;

        // Four of a Kind
        if (groups[0].Count() == 4)
            return 7_000_000 + groups[0].Key * 15 + groups[1].Key;

        // Full House
        if (groups[0].Count() == 3 && groups[1].Count() == 2)
            return 6_000_000 + groups[0].Key * 15 + groups[1].Key;

        // Flush
        if (isFlush)
            return 5_000_000 + highCard;

        // Straight
        if (isStraight)
            return 4_000_000 + highCard;

        // Three of a Kind
        if (groups[0].Count() == 3)
            return 3_000_000 + groups[0].Key * 225 + highCard % 225;

        // Two Pair
        if (groups[0].Count() == 2 && groups[1].Count() == 2)
            return 2_000_000 + Math.Max(groups[0].Key, groups[1].Key) * 225
                + Math.Min(groups[0].Key, groups[1].Key) * 15 + groups[2].Key;

        // Pair
        if (groups[0].Count() == 2)
            return 1_000_000 + groups[0].Key * 225 + highCard % 225;

        // High Card
        return highCard;
    }

    private int HandValue(List<string> hand)
    {
        int value = 0;
        int aces = 0;

        foreach (var card in hand)
        {
            var face = card[..^1]; // Remove suit
            if (face == "A")
            {
                aces++;
                value += 11;
            }
            else if (face is "K" or "Q" or "J")
                value += 10;
            else
                value += int.Parse(face);
        }

        while (value > 21 && aces > 0)
        {
            value -= 10;
            aces--;
        }

        return value;
    }
    #endregion

    #region Poker (Simplified)
    public class PokerGame
    {
        public ulong ChannelId { get; set; }
        public long Bet { get; set; }
        public List<(ulong UserId, string Username)> Players { get; set; } = new();
        public bool Started { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public PokerGame GetOrCreatePokerGame(ulong channelId, ulong userId, string username, long bet)
    {
        return ActivePokerGames.GetOrAdd(channelId, _ => new PokerGame
        {
            ChannelId = channelId,
            Bet = bet,
            Players = new() { (userId, username) }
        });
    }

    public bool JoinPokerGame(ulong channelId, ulong userId, string username)
    {
        if (!ActivePokerGames.TryGetValue(channelId, out var game) || game.Started)
            return false;
        if (game.Players.Count >= 6 || game.Players.Any(p => p.UserId == userId))
            return false;

        game.Players.Add((userId, username));
        return true;
    }

    public async Task<(ulong WinnerId, string WinnerName, long Pot, string Summary)> ResolvePokerAsync(ulong channelId)
    {
        if (!ActivePokerGames.TryRemove(channelId, out var game))
            return (0, "", 0, "No game found!");

        if (game.Players.Count < 2)
            return (0, "", 0, "Not enough players!");

        // Real poker hand evaluation
        var results = new List<(ulong UserId, string Username, int Score, string Hand)>();
        foreach (var (uid, uname) in game.Players)
        {
            var hand = new List<string>();
            for (int i = 0; i < 5; i++) hand.Add(DrawCard());
            var score = EvaluatePokerHand(hand);
            results.Add((uid, uname, score, string.Join(" ", hand)));
        }

        var winner = results.OrderByDescending(r => r.Score).First();
        var pot = game.Bet * game.Players.Count;

        // Remove bets from all players (check each one succeeds)
        long actualPot = 0;
        foreach (var p in game.Players)
        {
            var taken = await _cs.RemoveAsync(p.UserId, game.Bet, new TxData("poker", "bet"));
            if (taken) actualPot += game.Bet;
        }
        await _cs.AddAsync(winner.UserId, actualPot, new TxData("poker", "win"));

        var handRankNames = new[] { "High Card", "Pair", "Two Pair", "Three of a Kind", "Straight", "Flush", "Full House", "Four of a Kind", "Straight Flush", "Royal Flush" };
        var summary = string.Join("\n", results.OrderByDescending(r => r.Score)
            .Select((r, i) =>
            {
                var rankIdx = Math.Min(r.Score / 1_000_000, 9);
                var rankName = handRankNames[rankIdx];
                return $"{(i == 0 ? "👑" : "  ")} {r.Username}: {r.Hand} — **{rankName}**";
            }));

        return (winner.UserId, winner.Username, actualPot, summary);
    }
    #endregion
}
