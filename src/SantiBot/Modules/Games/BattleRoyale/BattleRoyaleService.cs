#nullable disable
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.BattleRoyale;

public sealed class BattleRoyaleService : INService
{
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, BRGame> ActiveGames = new();

    private static readonly string[] Events =
    [
        "{0} found a legendary weapon!",
        "{0} set a deadly trap!",
        "{0} found a health pack!",
        "{0} fell into a pit but survived!",
        "{0} ambushed {1} from behind!",
        "{0} and {1} fought — {0} won!",
        "{0} sniped {1} from a distance!",
        "{0} outsmarted {1} in close combat!",
        "A storm forced {0} to relocate.",
        "{0} found shelter and rested.",
        "{0} scavenged supplies from an abandoned building.",
        "{0} barely avoided a landmine!",
        "{0} challenged {1} to a duel and won!",
        "{0} poisoned {1}'s water supply!",
        "An airdrop landed near {0}!",
    ];

    public BattleRoyaleService(ICurrencyService cs)
    {
        _cs = cs;
    }

    public class BRGame
    {
        public ulong ChannelId { get; set; }
        public long EntryFee { get; set; }
        public List<(ulong UserId, string Username)> Players { get; set; } = new();
        public List<(ulong UserId, string Username)> Alive { get; set; } = new();
        public bool Started { get; set; }
        public int Round { get; set; }
    }

    public (bool Success, string Message) StartGame(ulong channelId, long entryFee)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A Battle Royale is already running!");

        ActiveGames[channelId] = new BRGame { ChannelId = channelId, EntryFee = entryFee };
        return (true, $"⚔️ **Battle Royale!** Entry fee: {entryFee} 🥠\nUse `.br join` to enter! Host uses `.br go` to start.");
    }

    public async Task<(bool Success, string Message)> JoinGame(ulong channelId, ulong userId, string username)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game) || game.Started)
            return (false, "No game or already started!");

        if (game.Players.Any(p => p.UserId == userId))
            return (false, "Already joined!");

        if (game.EntryFee > 0)
        {
            var removed = await _cs.RemoveAsync(userId, game.EntryFee, new TxData("br", "entry"));
            if (!removed) return (false, $"You need {game.EntryFee} 🥠!");
        }

        game.Players.Add((userId, username));
        return (true, $"{username} entered the Battle Royale! ({game.Players.Count} tributes)");
    }

    public async Task<(bool Success, string Message)> RunRoundAsync(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game!");

        if (!game.Started)
        {
            if (game.Players.Count < 2)
                return (false, "Need at least 2 players!");
            game.Started = true;
            game.Alive = new(game.Players);
        }

        game.Round++;
        var sb = new StringBuilder();
        sb.AppendLine($"⚔️ **Round {game.Round}** — {game.Alive.Count} tributes remain\n");

        // Process events
        var toRemove = new List<(ulong, string)>();
        var processed = new HashSet<ulong>();

        foreach (var player in game.Alive.ToList())
        {
            if (processed.Contains(player.UserId) || toRemove.Any(t => t.Item1 == player.UserId))
                continue;

            processed.Add(player.UserId);

            var eventType = _rng.Next(Events.Length);
            var evt = Events[eventType];

            if (evt.Contains("{1}"))
            {
                // PvP event - pick another alive player
                var others = game.Alive.Where(p => p.UserId != player.UserId && !processed.Contains(p.UserId) && !toRemove.Any(t => t.Item1 == p.UserId)).ToList();
                if (others.Count > 0)
                {
                    var victim = others[_rng.Next(others.Count)];
                    processed.Add(victim.UserId);
                    sb.AppendLine(string.Format(evt, $"**{player.Username}**", $"**{victim.Username}**"));
                    toRemove.Add(victim);
                }
                else
                {
                    sb.AppendLine($"**{player.Username}** searched for opponents but found none.");
                }
            }
            else
            {
                sb.AppendLine(string.Format(evt, $"**{player.Username}**"));
                // Small chance of self-elimination from dangerous events
                if (evt.Contains("pit") || evt.Contains("landmine"))
                {
                    if (_rng.Next(3) == 0)
                    {
                        sb.AppendLine($"  ...but **{player.Username}** didn't make it!");
                        toRemove.Add(player);
                    }
                }
            }
        }

        // If no one died this round, force an elimination
        if (toRemove.Count == 0 && game.Alive.Count > 1)
        {
            var unlucky = game.Alive[_rng.Next(game.Alive.Count)];
            sb.AppendLine($"\n💀 The zone closed in on **{unlucky.Username}**!");
            toRemove.Add(unlucky);
        }

        foreach (var dead in toRemove)
            game.Alive.RemoveAll(p => p.UserId == dead.Item1);

        if (game.Alive.Count <= 1)
        {
            if (game.Alive.Count == 1)
            {
                var winner = game.Alive[0];
                var pot = game.EntryFee * game.Players.Count;
                var prize = pot > 0 ? pot : 500;
                await _cs.AddAsync(winner.UserId, prize, new TxData("br", "win"));
                sb.AppendLine($"\n🏆 **{winner.Username}** is the last one standing! They win {prize} 🥠!");
            }
            else
            {
                sb.AppendLine("\n💀 Everyone perished! No winner.");
            }
            ActiveGames.TryRemove(channelId, out _);
        }
        else
        {
            sb.AppendLine($"\n{game.Alive.Count} tributes remain. Use `.br go` for next round!");
        }

        return (true, sb.ToString());
    }
}
