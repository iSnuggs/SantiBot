#nullable disable
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.HungerGames;

public sealed class HungerGamesService : INService
{
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, HGGame> ActiveGames = new();

    public HungerGamesService(ICurrencyService cs)
    {
        _cs = cs;
    }

    private static readonly string[] DayEvents =
    [
        "{0} finds a hidden cache of supplies.",
        "{0} crafts a makeshift weapon from branches.",
        "{0} discovers a stream and refills their water.",
        "{0} climbs a tree to scout the area.",
        "{0} sets up a camp in a cave.",
        "{0} hears a cannon in the distance.",
        "{0} runs into {1} — they decide to form an alliance!",
        "{0} ambushes {1} from the bushes!",
        "{0} challenges {1} to a fight at the cornucopia!",
        "{0} finds {1}'s camp and raids their supplies!",
        "{0} and {1} stumble upon each other — {0} is faster!",
        "{0} tracks {1} through the forest and attacks!",
    ];

    private static readonly string[] NightEvents =
    [
        "{0} huddles by a small fire through the night.",
        "{0} barely survives the freezing night.",
        "{0} is kept awake by distant screams.",
        "{0} dreams of home and wakes determined.",
        "{0} sneaks up on {1} while they sleep!",
        "{0} and {1} cross paths in the dark — {0} strikes first!",
    ];

    private static readonly string[] SpecialEvents =
    [
        "A wildfire sweeps through the arena! {0} doesn't make it out!",
        "Poisonous fog rolls in! {0} is caught in it!",
        "A pack of mutant wolves attacks! {0} can't escape!",
        "The gamemakers trigger a flood! {0} is swept away!",
        "A sponsor sends {0} a gift — they gain strength!",
    ];

    public class HGGame
    {
        public ulong ChannelId { get; set; }
        public List<(ulong UserId, string Name)> Tributes { get; set; } = new();
        public List<(ulong UserId, string Name)> Alive { get; set; } = new();
        public int Day { get; set; }
        public bool Started { get; set; }
    }

    public (bool Success, string Message) StartGame(ulong channelId)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A Hunger Games is already running!");

        ActiveGames[channelId] = new HGGame { ChannelId = channelId };
        return (true, "🏟️ **The Hunger Games!**\nUse `.hg join` or `.hg volunteer @user` to add tributes. `.hg go` to begin!");
    }

    public (bool Success, string Message) Join(ulong channelId, ulong userId, string name)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game) || game.Started)
            return (false, "No game or already started!");

        if (game.Tributes.Any(t => t.UserId == userId))
            return (false, "Already a tribute!");

        game.Tributes.Add((userId, name));
        return (true, $"**{name}** volunteers as tribute! ({game.Tributes.Count} tributes)");
    }

    public (bool Success, string Message) Volunteer(ulong channelId, ulong userId, string name)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game) || game.Started)
            return (false, "No game or already started!");

        if (game.Tributes.Any(t => t.UserId == userId))
            return (false, "Already a tribute!");

        game.Tributes.Add((userId, name));
        return (true, $"**{name}** has been volunteered! ({game.Tributes.Count} tributes)");
    }

    public async Task<(bool Success, string Message)> RunRoundAsync(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game!");

        if (!game.Started)
        {
            if (game.Tributes.Count < 3)
                return (false, "Need at least 3 tributes!");
            game.Started = true;
            game.Alive = new(game.Tributes);
            game.Day = 0;
        }

        game.Day++;
        var isDay = game.Day % 2 == 1;
        var events = isDay ? DayEvents : NightEvents;
        var sb = new StringBuilder();
        sb.AppendLine(isDay ? $"☀️ **Day {(game.Day + 1) / 2}**" : $"🌙 **Night {game.Day / 2}**");
        sb.AppendLine();

        var dead = new List<(ulong, string)>();
        var processed = new HashSet<ulong>();

        // Process events for each alive tribute
        foreach (var tribute in game.Alive.ToList())
        {
            if (processed.Contains(tribute.UserId) || dead.Any(d => d.Item1 == tribute.UserId))
                continue;

            processed.Add(tribute.UserId);

            // 15% chance of special event
            if (_rng.Next(100) < 15)
            {
                var special = SpecialEvents[_rng.Next(SpecialEvents.Length)];
                if (special.Contains("gift") || special.Contains("strength"))
                {
                    sb.AppendLine(string.Format(special, $"**{tribute.Name}**"));
                    continue;
                }
                else
                {
                    sb.AppendLine(string.Format(special, $"**{tribute.Name}**"));
                    dead.Add(tribute);
                    continue;
                }
            }

            var evt = events[_rng.Next(events.Length)];

            if (evt.Contains("{1}"))
            {
                var others = game.Alive.Where(t => t.UserId != tribute.UserId && !processed.Contains(t.UserId) && !dead.Any(d => d.Item1 == t.UserId)).ToList();
                if (others.Count > 0)
                {
                    var target = others[_rng.Next(others.Count)];
                    processed.Add(target.UserId);

                    if (evt.Contains("alliance"))
                    {
                        sb.AppendLine(string.Format(evt, $"**{tribute.Name}**", $"**{target.Name}**"));
                    }
                    else
                    {
                        // PvP — target dies
                        sb.AppendLine(string.Format(evt, $"**{tribute.Name}**", $"**{target.Name}**"));
                        dead.Add(target);
                    }
                }
                else
                {
                    sb.AppendLine($"**{tribute.Name}** searches for others but finds no one.");
                }
            }
            else
            {
                sb.AppendLine(string.Format(evt, $"**{tribute.Name}**"));
            }
        }

        // Remove dead
        foreach (var d in dead)
        {
            game.Alive.RemoveAll(t => t.UserId == d.Item1);
            sb.AppendLine($"💀 **{d.Item2}** has fallen!");
        }

        // Force kill if no one died this round
        if (dead.Count == 0 && game.Alive.Count > 1 && game.Day > 4)
        {
            var unlucky = game.Alive[_rng.Next(game.Alive.Count)];
            game.Alive.RemoveAll(t => t.UserId == unlucky.UserId);
            sb.AppendLine($"\n⚡ The gamemakers intervene! **{unlucky.Name}** is struck down!");
        }

        sb.AppendLine($"\n**{game.Alive.Count} tributes remain.**");

        if (game.Alive.Count <= 1)
        {
            if (game.Alive.Count == 1)
            {
                var winner = game.Alive[0];
                await _cs.AddAsync(winner.UserId, 50, new TxData("hg", "win"));
                sb.AppendLine($"\n🏆 **{winner.Name}** is the Victor of the Hunger Games! +50 🥠!");
            }
            else
            {
                sb.AppendLine("\n💀 No survivors. The Capitol is displeased.");
            }
            ActiveGames.TryRemove(channelId, out _);
        }

        return (true, sb.ToString());
    }
}
