#nullable disable
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Games.Mafia;

public sealed class MafiaService : INService
{
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, MafiaGame> ActiveGames = new();

    public MafiaService(ICurrencyService cs)
    {
        _cs = cs;
    }

    public enum MafiaRole { Villager, Mafia, Doctor, Detective }
    public enum GamePhase { Lobby, Day, Night, Ended }

    public class MafiaPlayer
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public MafiaRole Role { get; set; }
        public bool IsAlive { get; set; } = true;
    }

    public class MafiaGame
    {
        public ulong ChannelId { get; set; }
        public List<MafiaPlayer> Players { get; set; } = new();
        public GamePhase Phase { get; set; } = GamePhase.Lobby;
        public Dictionary<ulong, ulong> Votes { get; set; } = new(); // voter -> target
        public ulong MafiaTarget { get; set; }
        public ulong DoctorSave { get; set; }
        public ulong DetectiveInvestigate { get; set; }
        public int DayNumber { get; set; }
    }

    public (bool Success, string Message) StartGame(ulong channelId, ulong userId, string username)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A Mafia game is already running!");

        var game = new MafiaGame
        {
            ChannelId = channelId,
            Players = new() { new MafiaPlayer { UserId = userId, Username = username } }
        };

        ActiveGames[channelId] = game;
        return (true, "🐺 **Mafia game created!** Use `.mafia join` to join. Need at least 4 players. Host uses `.mafia begin` to start.");
    }

    public (bool Success, string Message) JoinGame(ulong channelId, ulong userId, string username)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game running!");

        if (game.Phase != GamePhase.Lobby)
            return (false, "Game already started!");

        if (game.Players.Any(p => p.UserId == userId))
            return (false, "Already joined!");

        if (game.Players.Count >= 12)
            return (false, "Game is full (max 12)!");

        game.Players.Add(new MafiaPlayer { UserId = userId, Username = username });
        return (true, $"{username} joined! ({game.Players.Count} players)");
    }

    public (bool Success, string Message) BeginGame(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game!");

        if (game.Players.Count < 4)
            return (false, "Need at least 4 players!");

        // Assign roles
        var shuffled = game.Players.OrderBy(_ => _rng.Next()).ToList();
        var mafiaCount = Math.Max(1, shuffled.Count / 4);

        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i < mafiaCount)
                shuffled[i].Role = MafiaRole.Mafia;
            else if (i == mafiaCount)
                shuffled[i].Role = MafiaRole.Doctor;
            else if (i == mafiaCount + 1)
                shuffled[i].Role = MafiaRole.Detective;
            else
                shuffled[i].Role = MafiaRole.Villager;
        }

        game.Phase = GamePhase.Day;
        game.DayNumber = 1;
        game.Votes.Clear();

        var roleList = string.Join("\n", game.Players.Select(p => $"<@{p.UserId}>: Role sent via DM (pretend)"));

        return (true, $"🌅 **Day 1 begins!** {game.Players.Count} players, {mafiaCount} mafia among you.\nRoles have been assigned! Use `.mafia vote @user` to vote someone out.\n\nAlive: {string.Join(", ", game.Players.Where(p => p.IsAlive).Select(p => p.Username))}");
    }

    public (bool Success, string Message) Vote(ulong channelId, ulong voterId, ulong targetId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game!");

        if (game.Phase != GamePhase.Day)
            return (false, "Voting only during the day!");

        var voter = game.Players.FirstOrDefault(p => p.UserId == voterId && p.IsAlive);
        var target = game.Players.FirstOrDefault(p => p.UserId == targetId && p.IsAlive);

        if (voter is null) return (false, "You're not alive in this game!");
        if (target is null) return (false, "That player isn't alive!");

        game.Votes[voterId] = targetId;

        var alivePlayers = game.Players.Count(p => p.IsAlive);
        var votesNeeded = alivePlayers / 2 + 1;
        var voteCount = game.Votes.Count;

        return (true, $"{voter.Username} voted for {target.Username}! ({voteCount}/{votesNeeded} votes cast)");
    }

#pragma warning disable CS1998 // Async method lacks 'await' — all logic is synchronous
    public async Task<(bool Success, string Message)> ResolvePhaseAsync(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game!");

        if (game.Phase == GamePhase.Day)
        {
            // Tally votes
            if (game.Votes.Count == 0)
                return (false, "No votes cast yet!");

            var tally = game.Votes.GroupBy(v => v.Value)
                .OrderByDescending(g => g.Count())
                .First();

            var eliminated = game.Players.First(p => p.UserId == tally.Key);
            eliminated.IsAlive = false;

            var result = $"🗳️ **{eliminated.Username}** was voted out! They were a **{eliminated.Role}**!";

            // Check win conditions
            var winCheck = CheckWinCondition(game);
            if (winCheck is not null)
            {
                ActiveGames.TryRemove(channelId, out _);
                return (true, result + "\n" + winCheck);
            }

            game.Phase = GamePhase.Night;
            game.Votes.Clear();
            game.MafiaTarget = 0;
            game.DoctorSave = 0;

            return (true, result + "\n\n🌙 **Night falls!** Mafia, Doctor, and Detective make their moves.\nUse `.mafia nightaction @user` for your night action, then `.mafia dawn` to resolve.");
        }
        else if (game.Phase == GamePhase.Night)
        {
            // Resolve night
            var killed = game.Players.FirstOrDefault(p => p.UserId == game.MafiaTarget && p.IsAlive);
            string nightResult;

            if (killed is not null && game.DoctorSave != killed.UserId)
            {
                killed.IsAlive = false;
                nightResult = $"☠️ **{killed.Username}** was killed in the night!";
            }
            else if (killed is not null)
            {
                nightResult = "🩺 Someone was attacked but the **Doctor** saved them!";
            }
            else
            {
                nightResult = "The night passes peacefully...";
            }

            var winCheck = CheckWinCondition(game);
            if (winCheck is not null)
            {
                ActiveGames.TryRemove(channelId, out _);
                return (true, nightResult + "\n" + winCheck);
            }

            game.Phase = GamePhase.Day;
            game.DayNumber++;
            game.Votes.Clear();

            var alive = string.Join(", ", game.Players.Where(p => p.IsAlive).Select(p => p.Username));
            return (true, $"{nightResult}\n\n🌅 **Day {game.DayNumber}!** Alive: {alive}\nVote with `.mafia vote @user`.");
        }

        return (false, "Can't resolve in current phase.");
    }
#pragma warning restore CS1998

    public (bool Success, string Message) NightAction(ulong channelId, ulong userId, ulong targetId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game) || game.Phase != GamePhase.Night)
            return (false, "Not night phase!");

        var player = game.Players.FirstOrDefault(p => p.UserId == userId && p.IsAlive);
        if (player is null) return (false, "You're not alive!");

        switch (player.Role)
        {
            case MafiaRole.Mafia:
                game.MafiaTarget = targetId;
                return (true, "Mafia has chosen their target.");

            case MafiaRole.Doctor:
                game.DoctorSave = targetId;
                return (true, "Doctor will protect someone tonight.");

            case MafiaRole.Detective:
                game.DetectiveInvestigate = targetId;
                var investigated = game.Players.FirstOrDefault(p => p.UserId == targetId);
                if (investigated is not null)
                    return (true, $"🔍 Investigation: **{investigated.Username}** is a **{investigated.Role}**!");
                return (false, "Target not found!");

            default:
                return (false, "You don't have a night action!");
        }
    }

    private string CheckWinCondition(MafiaGame game)
    {
        var aliveMafia = game.Players.Count(p => p.IsAlive && p.Role == MafiaRole.Mafia);
        var aliveVillagers = game.Players.Count(p => p.IsAlive && p.Role != MafiaRole.Mafia);

        if (aliveMafia == 0)
            return "🎉 **Villagers win!** All mafia have been eliminated!";

        if (aliveMafia >= aliveVillagers)
            return "🐺 **Mafia wins!** They outnumber the villagers!";

        return null;
    }
}
