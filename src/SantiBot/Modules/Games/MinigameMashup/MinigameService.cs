#nullable disable
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Games.MinigameMashup;

public sealed class MinigameService : INService
{
    private readonly ICurrencyService _cs;
    private readonly DiscordSocketClient _client;
    private static readonly SantiRandom _rng = new();

    // Active minigames per channel — (type, answer, startTime, reward)
    public readonly ConcurrentDictionary<ulong, (string Type, string Answer, DateTime Started, long Reward)> ActiveMinigames = new();

    private static readonly string[] Words =
    [
        "DISCORD", "FORTUNE", "COOKIE", "GAMING", "STREAM", "DRAGON", "CASTLE",
        "WIZARD", "KNIGHT", "PIRATE", "THUNDER", "CRYSTAL", "SHADOW", "PHOENIX",
        "GALAXY", "NEBULA", "ROCKET", "PLANET", "COMET", "ASTEROID",
    ];

    private static readonly string[] TypeRacePhrases =
    [
        "The quick brown fox jumps over the lazy dog",
        "Fortune cookies bring good luck to all",
        "Gaming is the best way to spend time",
        "Dragons are the coolest mythical creatures",
        "Never gonna give you up never gonna let you down",
        "The cake is a lie but the cookies are real",
        "May the force be with you always",
        "To be or not to be that is the question",
    ];

    public MinigameService(ICurrencyService cs, DiscordSocketClient client)
    {
        _cs = cs;
        _client = client;
    }

    public (string Type, string Challenge, string Answer) GenerateMinigame()
    {
        var type = _rng.Next(4);

        switch (type)
        {
            case 0: // TypeRace
            {
                var phrase = TypeRacePhrases[_rng.Next(TypeRacePhrases.Length)];
                return ("TypeRace", $"⌨️ **Type Race!** First to type this phrase wins:\n\n`{phrase}`", phrase);
            }
            case 1: // MathBlitz
            {
                var a = _rng.Next(10, 100);
                var b = _rng.Next(10, 100);
                var op = _rng.Next(3);
                string question;
                int answer;

                switch (op)
                {
                    case 0:
                        answer = a + b;
                        question = $"{a} + {b}";
                        break;
                    case 1:
                        answer = a * b;
                        question = $"{a} × {b}";
                        break;
                    default:
                        answer = a - b;
                        question = $"{a} - {b}";
                        break;
                }

                return ("MathBlitz", $"🧮 **Math Blitz!** Solve this:\n\n**{question} = ?**", answer.ToString());
            }
            case 2: // Unscramble
            {
                var word = Words[_rng.Next(Words.Length)];
                var scrambled = new string(word.OrderBy(_ => _rng.Next()).ToArray());
                // Ensure it's actually scrambled
                while (scrambled == word)
                    scrambled = new string(word.OrderBy(_ => _rng.Next()).ToArray());

                return ("Unscramble", $"🔀 **Unscramble!** What word is this?\n\n**{scrambled}**", word);
            }
            default: // ReactionTest
            {
                var secretWord = Words[_rng.Next(Words.Length)];
                return ("ReactionTest", $"⚡ **Reaction Test!** First to type this word wins:\n\n**{secretWord}**", secretWord);
            }
        }
    }

    public async Task<(bool Won, string Message)> TryAnswerAsync(ulong channelId, ulong userId, string answer)
    {
        if (!ActiveMinigames.TryGetValue(channelId, out var game))
            return (false, "");

        var isCorrect = game.Answer.Equals(answer.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!isCorrect)
            return (false, "");

        if (!ActiveMinigames.TryRemove(channelId, out _))
            return (false, "");

        var timeTaken = (DateTime.UtcNow - game.Started).TotalSeconds;
        var reward = Math.Max(50, (long)(game.Reward - timeTaken * 5));

        await _cs.AddAsync(userId, reward, new TxData("minigame", "win"));

        return (true, $"🎉 <@{userId}> wins the **{game.Type}**! ({timeTaken:F1}s) +{reward} 🥠!");
    }

    public (bool Success, string Challenge) StartMinigame(ulong channelId)
    {
        if (ActiveMinigames.ContainsKey(channelId))
            return (false, "A minigame is already running! Answer it first!");

        var (type, challenge, answer) = GenerateMinigame();
        ActiveMinigames[channelId] = (type, answer, DateTime.UtcNow, 200);

        return (true, challenge);
    }
}
