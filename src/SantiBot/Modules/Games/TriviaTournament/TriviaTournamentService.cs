#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Games.TriviaTournament;

public sealed class TriviaTournamentService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    public readonly ConcurrentDictionary<ulong, TournamentGame> ActiveTournaments = new();

    private static readonly Dictionary<string, (string Question, string Answer)[]> Categories = new()
    {
        ["General"] =
        [
            ("What is the largest planet in our solar system?", "Jupiter"),
            ("How many continents are there?", "7"),
            ("What year did World War II end?", "1945"),
            ("What is the chemical symbol for gold?", "Au"),
            ("Who painted the Mona Lisa?", "Leonardo da Vinci"),
            ("What is the smallest country in the world?", "Vatican City"),
            ("How many bones are in the human body?", "206"),
            ("What is the speed of light in km/s?", "300000"),
        ],
        ["Science"] =
        [
            ("What is H2O?", "Water"),
            ("What planet is known as the Red Planet?", "Mars"),
            ("What is the powerhouse of the cell?", "Mitochondria"),
            ("What gas do plants absorb?", "Carbon dioxide"),
            ("What is the chemical symbol for sodium?", "Na"),
            ("How many chromosomes do humans have?", "46"),
            ("What is the hardest natural substance?", "Diamond"),
            ("What force keeps us on the ground?", "Gravity"),
        ],
        ["History"] =
        [
            ("Who was the first US president?", "George Washington"),
            ("In what year did the Titanic sink?", "1912"),
            ("Who discovered America?", "Columbus"),
            ("What empire built the Colosseum?", "Roman"),
            ("What year was the Declaration of Independence signed?", "1776"),
            ("Who was the Egyptian queen known for beauty?", "Cleopatra"),
            ("What wall fell in 1989?", "Berlin Wall"),
            ("What ancient wonder was in Alexandria?", "Lighthouse"),
        ],
        ["Gaming"] =
        [
            ("What is the best-selling video game of all time?", "Minecraft"),
            ("Who is Mario's brother?", "Luigi"),
            ("What game features a battle royale on an island?", "Fortnite"),
            ("What game has Creepers?", "Minecraft"),
            ("Who is the protagonist of The Legend of Zelda?", "Link"),
            ("What game popularized battle royale?", "PUBG"),
            ("What Pokemon is #1 in the Pokedex?", "Bulbasaur"),
            ("What company made the PlayStation?", "Sony"),
        ],
        ["Anime"] =
        [
            ("Who is the main character of Naruto?", "Naruto"),
            ("What anime features a Death Note?", "Death Note"),
            ("Who is Goku's rival in Dragon Ball Z?", "Vegeta"),
            ("What anime has titans attacking walls?", "Attack on Titan"),
            ("Who is the pirate king in One Piece?", "Gol D. Roger"),
            ("What studio made Spirited Away?", "Ghibli"),
            ("What is Luffy's Devil Fruit power?", "Rubber"),
            ("Who is the strongest hero in One Punch Man?", "Saitama"),
        ],
    };

    public TriviaTournamentService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public class TournamentGame
    {
        public ulong ChannelId { get; set; }
        public string Category { get; set; }
        public long EntryFee { get; set; }
        public List<(ulong UserId, string Username)> Players { get; set; } = new();
        public bool Started { get; set; }
        public int CurrentRound { get; set; }
        public Dictionary<ulong, int> Scores { get; set; } = new();
        public (string Question, string Answer)? CurrentQuestion { get; set; }
        public DateTime QuestionAskedAt { get; set; }
    }

    public (bool Success, string Message) StartTournament(ulong channelId, string category, long entryFee, ulong userId, string username)
    {
        if (!Categories.ContainsKey(category))
            return (false, $"Unknown category. Available: {string.Join(", ", Categories.Keys)}");

        if (ActiveTournaments.ContainsKey(channelId))
            return (false, "A tournament is already running!");

        var game = new TournamentGame
        {
            ChannelId = channelId,
            Category = category,
            EntryFee = entryFee,
            Players = new() { (userId, username) },
        };

        ActiveTournaments[channelId] = game;
        return (true, $"🏆 **Trivia Tournament!** Category: {category}, Entry: {entryFee} 🥠\nUse `.triviatour join` to enter! Max 8 players.");
    }

    public async Task<(bool Success, string Message)> JoinTournament(ulong channelId, ulong userId, string username)
    {
        if (!ActiveTournaments.TryGetValue(channelId, out var game) || game.Started)
            return (false, "No tournament to join or already started!");

        if (game.Players.Count >= 8)
            return (false, "Tournament is full!");

        if (game.Players.Any(p => p.UserId == userId))
            return (false, "Already joined!");

        if (game.EntryFee > 0)
        {
            var removed = await _cs.RemoveAsync(userId, game.EntryFee, new TxData("trivia", "entry"));
            if (!removed)
                return (false, $"You need {game.EntryFee} 🥠 to enter!");
        }

        game.Players.Add((userId, username));
        return (true, $"{username} joined! ({game.Players.Count}/8 players)");
    }

    public (bool Success, string Question) NextQuestion(ulong channelId)
    {
        if (!ActiveTournaments.TryGetValue(channelId, out var game))
            return (false, "No tournament!");

        game.Started = true;
        game.CurrentRound++;

        var questions = Categories[game.Category];
        var q = questions[_rng.Next(questions.Length)];
        game.CurrentQuestion = q;
        game.QuestionAskedAt = DateTime.UtcNow;

        return (true, $"**Round {game.CurrentRound}:** {q.Question}");
    }

    public async Task<(bool Correct, string Message)> AnswerQuestion(ulong channelId, ulong userId, string answer)
    {
        if (!ActiveTournaments.TryGetValue(channelId, out var game) || game.CurrentQuestion is null)
            return (false, "");

        if (!game.Players.Any(p => p.UserId == userId))
            return (false, "");

        var correct = game.CurrentQuestion.Value.Answer.Equals(answer, StringComparison.OrdinalIgnoreCase);
        if (!correct)
            return (false, "");

        game.Scores.TryGetValue(userId, out var score);
        game.Scores[userId] = score + 1;
        game.CurrentQuestion = null;

        var username = game.Players.First(p => p.UserId == userId).Username;

        if (game.CurrentRound >= 5)
        {
            // Tournament over
            var winner = game.Scores.OrderByDescending(x => x.Value).First();
            var pot = game.EntryFee * game.Players.Count;
            await _cs.AddAsync(winner.Key, pot > 0 ? pot : 500, new TxData("trivia", "win"));
            ActiveTournaments.TryRemove(channelId, out _);

            var winnerName = game.Players.First(p => p.UserId == winner.Key).Username;
            return (true, $"Correct, {username}!\n\n🏆 **Tournament Over!** Winner: **{winnerName}** with {winner.Value} points! Prize: {(pot > 0 ? pot : 500)} 🥠");
        }

        return (true, $"Correct, {username}! (+1 point)\nNext question coming...");
    }

    public async Task<List<TriviaTournamentEntry>> GetLeaderboardAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<TriviaTournamentEntry>()
            .Where(e => e.GuildId == guildId)
            .OrderByDescending(e => e.Wins)
            .Take(10)
            .ToListAsyncLinqToDB();
    }
}
