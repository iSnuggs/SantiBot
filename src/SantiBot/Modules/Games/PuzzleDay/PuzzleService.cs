#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Games.PuzzleDay;

public sealed class PuzzleService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    private static readonly (string Question, string Answer, string Hint)[] Puzzles =
    [
        ("What has keys but no locks?", "piano", "It makes music"),
        ("What has a head and a tail but no body?", "coin", "You flip it"),
        ("I speak without a mouth and hear without ears. What am I?", "echo", "You hear it in canyons"),
        ("What can travel around the world while staying in a corner?", "stamp", "You put it on letters"),
        ("What has cities but no houses, forests but no trees, and water but no fish?", "map", "Navigation tool"),
        ("The more you take, the more you leave behind. What am I?", "footsteps", "Walking creates them"),
        ("What is always in front of you but can't be seen?", "future", "Time related"),
        ("What can you break even if you never pick it up?", "promise", "You make it with words"),
        ("What goes up but never comes down?", "age", "Everyone has it"),
        ("I have branches but no fruit, trunk, or leaves. What am I?", "bank", "Financial institution"),
        ("What is full of holes but still holds water?", "sponge", "Kitchen cleaning tool"),
        ("What can you catch but not throw?", "cold", "You get sick"),
        ("What has many teeth but can't bite?", "comb", "Hair styling tool"),
        ("What building has the most stories?", "library", "Full of books"),
        ("What tastes better than it smells?", "tongue", "It's on your face"),
        ("If 2+3=10, 7+2=63, 6+5=66, 8+4=96, then 9+7=?", "144", "Multiply first, then add first number"),
        ("What 5-letter word becomes shorter when you add 2 letters?", "short", "Add 'er' to it"),
        ("Complete: 1, 1, 2, 3, 5, 8, 13, ?", "21", "Fibonacci sequence"),
        ("A farmer has 17 sheep. All but 9 die. How many are left?", "9", "Read carefully — 'all but 9'"),
        ("What month has 28 days?", "all", "Every month has at least 28"),
        ("How many times can you subtract 5 from 25?", "1", "After that it's 20, not 25"),
        ("What word is spelled incorrectly in every dictionary?", "incorrectly", "Think literally"),
        ("I am an odd number. Take away a letter and I become even. What am I?", "seven", "Remove the 's'"),
        ("What has 4 eyes but can't see?", "mississippi", "It's a state"),
        ("What starts with 'e' and ends with 'e' but only has one letter in it?", "envelope", "You send it in the mail"),
        ("Complete: 2, 6, 12, 20, 30, ?", "42", "Differences increase by 2"),
        ("If you have it, you want to share it. If you share it, you don't have it. What is it?", "secret", "Confidential info"),
        ("What gets wetter the more it dries?", "towel", "You use it after a shower"),
        ("What can run but never walks?", "water", "Think of a river"),
        ("What has a ring but no finger?", "phone", "It buzzes in your pocket"),
    ];

    // Rate limit: 3 seconds between answer attempts
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(ulong, ulong), DateTime> _answerCooldowns = new();

    public PuzzleService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    private (string Question, string Answer, string Hint) GetTodaysPuzzle()
    {
        var dayOfYear = DateTime.UtcNow.DayOfYear;
        return Puzzles[dayOfYear % Puzzles.Length];
    }

    public (string Question, int PuzzleNumber) GetPuzzle()
    {
        var puzzle = GetTodaysPuzzle();
        return (puzzle.Question, DateTime.UtcNow.DayOfYear);
    }

    public async Task<(bool Success, string Message)> SolveAsync(ulong guildId, ulong userId, string answer)
    {
        // Rate limit: 3 seconds between attempts
        var key = (guildId, userId);
        if (_answerCooldowns.TryGetValue(key, out var lastAttempt))
        {
            if ((DateTime.UtcNow - lastAttempt).TotalSeconds < 3)
                return (false, "Too fast! Wait a few seconds between attempts.");
        }
        _answerCooldowns[key] = DateTime.UtcNow;

        var puzzle = GetTodaysPuzzle();
        var today = DateTime.UtcNow.Date;

        await using var ctx = _db.GetDbContext();
        var score = await ctx.GetTable<PuzzleScore>()
            .FirstOrDefaultAsyncLinqToDB(s => s.GuildId == guildId && s.UserId == userId);

        if (score is not null && score.LastSolvedDate.Date == today)
            return (false, "You already solved today's puzzle!");

        if (!answer.Equals(puzzle.Answer, StringComparison.OrdinalIgnoreCase))
            return (false, "Wrong answer! Try again or use `.puzzle hint`.");

        // Award points — earlier solvers get more
        var solvedCount = await ctx.GetTable<PuzzleScore>()
            .CountAsyncLinqToDB(s => s.GuildId == guildId && s.LastSolvedDate == today);

        var points = Math.Max(10, 100 - solvedCount * 10); // First solver gets 100, decreasing
        var reward = points * 2L;

        if (score is null)
        {
            await ctx.GetTable<PuzzleScore>().InsertAsync(() => new PuzzleScore
            {
                GuildId = guildId,
                UserId = userId,
                TotalSolved = 1,
                TotalPoints = points,
                LastSolvedDate = today,
                DateAdded = DateTime.UtcNow
            });
        }
        else
        {
            await ctx.GetTable<PuzzleScore>()
                .Where(s => s.Id == score.Id)
                .UpdateAsync(s => new PuzzleScore
                {
                    TotalSolved = score.TotalSolved + 1,
                    TotalPoints = score.TotalPoints + points,
                    LastSolvedDate = today
                });
        }

        await _cs.AddAsync(userId, reward, new TxData("puzzle", "solve"));

        return (true, $"Correct! **{puzzle.Answer}**! +{points} points, +{reward} 🥠! ({(solvedCount == 0 ? "First solver!" : $"Solver #{solvedCount + 1}")})");
    }

    public string GetHint()
    {
        var puzzle = GetTodaysPuzzle();
        return puzzle.Hint;
    }

    public async Task<List<PuzzleScore>> GetLeaderboardAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PuzzleScore>()
            .Where(s => s.GuildId == guildId)
            .OrderByDescending(s => s.TotalPoints)
            .Take(10)
            .ToListAsyncLinqToDB();
    }
}
