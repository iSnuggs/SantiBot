#nullable disable
namespace SantiBot.Modules.Games.MiniGames;

public sealed class MiniGameService : INService
{
    private readonly SantiRandom _rng = new();

    // ─── Active Game State ───────────────────
    // Wordle: ChannelId -> game state
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, WordleGame> _wordleGames = new();
    // Number Guess: ChannelId -> game state
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, NumberGuessGame> _guessGames = new();
    // Word Scramble: ChannelId -> game state
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, ScrambleGame> _scrambleGames = new();
    // Memory Game: ChannelId -> game state
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, MemoryGameState> _memoryGames = new();

    // ═══════════════════════════════════════════
    //  WORDLE
    // ═══════════════════════════════════════════

    public sealed class WordleGame
    {
        public string TargetWord { get; init; }
        public List<string> Guesses { get; } = new();
        public int MaxAttempts => 6;
        public bool IsOver { get; set; }
        public bool Won { get; set; }
    }

    private static readonly string[] WordleWords =
    [
        "about", "above", "abuse", "actor", "acute", "admit", "adopt", "adult", "after", "again",
        "agent", "agree", "ahead", "alarm", "album", "alert", "alien", "align", "alike", "alive",
        "alley", "allow", "alone", "along", "alter", "among", "angel", "anger", "angle", "angry",
        "anime", "apart", "apple", "apply", "arena", "argue", "arise", "armor", "array", "aside",
        "asset", "audit", "avoid", "awake", "award", "aware", "badge", "basic", "basis", "beach",
        "beast", "begin", "being", "below", "bench", "berry", "birth", "black", "blade", "blame",
        "blank", "blast", "blaze", "bleed", "blend", "bless", "blind", "block", "blood", "bloom",
        "blown", "blues", "board", "bonus", "boost", "booth", "bound", "brain", "brand", "brave",
        "bread", "break", "breed", "brick", "bride", "brief", "bring", "broad", "brook", "brown",
        "brush", "build", "burst", "buyer", "cabin", "cable", "candy", "carry", "catch", "cause",
        "chain", "chair", "chaos", "charm", "chart", "chase", "cheap", "check", "chess", "chest",
        "chief", "child", "china", "choir", "chunk", "civic", "civil", "claim", "clash", "class",
        "clean", "clear", "clerk", "click", "cliff", "climb", "cling", "clock", "clone", "close",
        "cloud", "coach", "coast", "color", "comet", "comic", "coral", "count", "court", "cover",
        "crack", "craft", "crane", "crash", "crawl", "crazy", "cream", "crime", "cross", "crowd",
        "crown", "cruel", "crush", "curve", "cycle", "daily", "dance", "death", "debug", "delay",
        "delta", "demon", "dense", "depot", "depth", "derby", "devil", "diary", "dirty", "disco",
        "ditch", "dodge", "doing", "donor", "doubt", "dough", "draft", "drain", "drake", "drama",
        "drank", "drawn", "dream", "dress", "dried", "drift", "drill", "drink", "drive", "droit",
        "drone", "drove", "dying", "eager", "eagle", "early", "earth", "eight", "elder", "elect",
        "elite", "ember", "empty", "enemy", "enjoy", "enter", "equal", "equip", "erase", "error",
        "essay", "event", "every", "exact", "exile", "exist", "extra", "fable", "faced", "faith",
        "false", "fancy", "fatal", "fault", "feast", "fence", "ferry", "fever", "fiber", "field",
        "fifty", "fight", "final", "first", "fixed", "flame", "flash", "flask", "fleet", "flesh",
        "flies", "float", "flood", "floor", "flour", "fluid", "flush", "flute", "focus", "force",
        "forge", "forth", "forum", "found", "frame", "frank", "fraud", "fresh", "front", "frost",
        "froze", "fruit", "gauge", "ghost", "giant", "given", "glass", "gleam", "glide", "globe",
        "gloom", "glory", "glove", "going", "grace", "grade", "grain", "grand", "grant", "graph",
        "grasp", "grass", "grave", "great", "greed", "green", "greet", "grief", "grill", "grind",
        "groan", "groom", "gross", "group", "grove", "growl", "grown", "guard", "guess", "guest",
        "guide", "guild", "guilt", "guise", "habit", "happy", "harsh", "haunt", "heart", "heavy",
        "hedge", "hello", "herbs", "honey", "honor", "horse", "hotel", "house", "human", "humor",
        "hurry", "ideal", "image", "imply", "index", "indie", "inner", "input", "irony", "issue",
        "ivory", "jewel", "joker", "joint", "judge", "juice", "keeps", "knife", "knock", "known",
        "label", "labor", "laser", "later", "laugh", "layer", "learn", "lease", "legal", "lemon",
        "level", "light", "limit", "linen", "liver", "local", "lodge", "logic", "login", "loose",
        "lorry", "lover", "lower", "lucky", "lunar", "lunch", "magic", "major", "maker", "manor",
        "march", "marry", "mason", "match", "mayor", "medal", "media", "mercy", "merit", "metal",
        "meter", "midst", "might", "minor", "minus", "mixer", "model", "money", "month", "moral",
        "motor", "mount", "mouse", "mouth", "movie", "music", "naive", "naval", "nerve", "never",
        "night", "noble", "noise", "north", "noted", "novel", "nurse", "occur", "ocean", "offer"
    ];

    public WordleGame StartWordle(ulong channelId)
    {
        var word = WordleWords[_rng.Next(WordleWords.Length)];
        var game = new WordleGame { TargetWord = word };

        if (!_wordleGames.TryAdd(channelId, game))
            return null; // game already in progress

        return game;
    }

    public (WordleGame game, string result) GuessWordle(ulong channelId, string guess)
    {
        if (!_wordleGames.TryGetValue(channelId, out var game))
            return (null, null);

        guess = guess.ToLowerInvariant().Trim();
        if (guess.Length != 5)
            return (game, "INVALID_LENGTH");

        game.Guesses.Add(guess);
        var result = EvaluateWordle(game.TargetWord, guess);

        if (guess == game.TargetWord)
        {
            game.IsOver = true;
            game.Won = true;
            _wordleGames.TryRemove(channelId, out _);
        }
        else if (game.Guesses.Count >= game.MaxAttempts)
        {
            game.IsOver = true;
            game.Won = false;
            _wordleGames.TryRemove(channelId, out _);
        }

        return (game, result);
    }

    public WordleGame GetWordleGame(ulong channelId)
        => _wordleGames.TryGetValue(channelId, out var g) ? g : null;

    public void EndWordle(ulong channelId)
        => _wordleGames.TryRemove(channelId, out _);

    private static string EvaluateWordle(string target, string guess)
    {
        var result = new char[5];
        var targetChars = target.ToCharArray();
        var used = new bool[5];

        // First pass: correct position (green)
        for (var i = 0; i < 5; i++)
        {
            if (guess[i] == targetChars[i])
            {
                result[i] = 'G'; // green
                used[i] = true;
            }
        }

        // Second pass: correct letter wrong position (yellow)
        for (var i = 0; i < 5; i++)
        {
            if (result[i] == 'G')
                continue;

            var found = false;
            for (var j = 0; j < 5; j++)
            {
                if (!used[j] && guess[i] == targetChars[j])
                {
                    result[i] = 'Y'; // yellow
                    used[j] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
                result[i] = 'B'; // black/gray
        }

        return new string(result);
    }

    public static string FormatWordleResult(string guess, string result)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 5; i++)
        {
            sb.Append(result[i] switch
            {
                'G' => "\ud83d\udfe9", // green square
                'Y' => "\ud83d\udfe8", // yellow square
                _ => "\u2b1c"          // white square
            });
        }
        sb.Append(' ');
        foreach (var c in guess)
            sb.Append($"**{char.ToUpper(c)}** ");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════
    //  MINESWEEPER
    // ═══════════════════════════════════════════

    public string GenerateMinesweeper(string difficulty)
    {
        int size;
        int mines;
        switch (difficulty.ToLowerInvariant())
        {
            case "easy":
                size = 5; mines = 4; break;
            case "medium":
                size = 8; mines = 12; break;
            case "hard":
                size = 10; mines = 20; break;
            default:
                size = 5; mines = 4; break;
        }

        var grid = new int[size, size];

        // Place mines
        var placed = 0;
        while (placed < mines)
        {
            var r = _rng.Next(size);
            var c = _rng.Next(size);
            if (grid[r, c] != -1)
            {
                grid[r, c] = -1;
                placed++;
            }
        }

        // Calculate numbers
        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                if (grid[r, c] == -1)
                    continue;

                var count = 0;
                for (var dr = -1; dr <= 1; dr++)
                {
                    for (var dc = -1; dc <= 1; dc++)
                    {
                        var nr = r + dr;
                        var nc = c + dc;
                        if (nr >= 0 && nr < size && nc >= 0 && nc < size && grid[nr, nc] == -1)
                            count++;
                    }
                }
                grid[r, c] = count;
            }
        }

        // Format as spoiler tags
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"**Minesweeper** ({difficulty}) — {size}x{size}, {mines} mines");
        for (var r = 0; r < size; r++)
        {
            for (var c = 0; c < size; c++)
            {
                var emoji = grid[r, c] switch
                {
                    -1 => "\ud83d\udca5", // bomb
                    0 => "\u2b1c",         // white square (0)
                    1 => "1\ufe0f\u20e3",
                    2 => "2\ufe0f\u20e3",
                    3 => "3\ufe0f\u20e3",
                    4 => "4\ufe0f\u20e3",
                    5 => "5\ufe0f\u20e3",
                    6 => "6\ufe0f\u20e3",
                    7 => "7\ufe0f\u20e3",
                    8 => "8\ufe0f\u20e3",
                    _ => "\u2b1c"
                };
                sb.Append($"||{emoji}||");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ═══════════════════════════════════════════
    //  NUMBER GUESS
    // ═══════════════════════════════════════════

    public sealed class NumberGuessGame
    {
        public int TargetNumber { get; init; }
        public int Attempts { get; set; }
        public int MaxRange => 1000;
    }

    public NumberGuessGame StartNumberGuess(ulong channelId)
    {
        var game = new NumberGuessGame
        {
            TargetNumber = _rng.Next(1, 1001) // 1-1000
        };

        if (!_guessGames.TryAdd(channelId, game))
            return null;

        return game;
    }

    public (NumberGuessGame game, string hint) GuessNumber(ulong channelId, int guess)
    {
        if (!_guessGames.TryGetValue(channelId, out var game))
            return (null, null);

        game.Attempts++;

        if (guess == game.TargetNumber)
        {
            _guessGames.TryRemove(channelId, out _);
            return (game, "CORRECT");
        }

        return (game, guess < game.TargetNumber ? "HIGHER" : "LOWER");
    }

    public NumberGuessGame GetGuessGame(ulong channelId)
        => _guessGames.TryGetValue(channelId, out var g) ? g : null;

    public void EndGuessGame(ulong channelId)
        => _guessGames.TryRemove(channelId, out _);

    // ═══════════════════════════════════════════
    //  WORD SCRAMBLE
    // ═══════════════════════════════════════════

    public sealed class ScrambleGame
    {
        public string OriginalWord { get; init; }
        public string ScrambledWord { get; init; }
    }

    private static readonly string[] ScrambleWords =
    [
        "adventure", "airplane", "alligator", "alphabet", "amplifier", "anchored", "asteroid",
        "backpack", "balanced", "barbecue", "baseball", "basement", "bathroom", "battery",
        "birthday", "blankets", "blizzard", "bookmark", "bracelet", "branches", "breakers",
        "brothers", "building", "bulletin", "business", "calendar", "campaign", "campfire",
        "canister", "captured", "cardinal", "carnival", "carousel", "catapult", "category",
        "cemetery", "champion", "chapters", "charcoal", "cheetahs", "chemical", "children",
        "chocolate", "cinnamon", "circuits", "climbing", "clothing", "clusters", "coaching",
        "coloring", "combined", "commerce", "compared", "compiler", "composed", "computer",
        "concerns", "conflict", "congress", "conquest", "consider", "contents", "contract",
        "cooldown", "corridor", "coverage", "creative", "criminal", "crossing", "crystals",
        "cupboard", "currency", "customer", "cylinder", "database", "daughter", "december",
        "decision", "declined", "defender", "delivery", "designed", "designer", "detailed",
        "detector", "diagonal", "diamonds", "dictator", "dinosaur", "diplomat", "director",
        "disabled", "disaster", "discount", "discover", "disguise", "document", "dolphins",
        "dominant", "doorbell", "download", "draftsman", "dramatic", "dripping", "driveway",
        "dynamite", "earphone", "economic", "educated", "election", "electric", "elephant",
        "elevated", "elevator", "embedded", "emerging", "emotions", "emphasis", "employed",
        "engineer", "enormous", "envelope", "equipped", "eruption", "escalate", "espresso",
        "estimate", "evaluate", "eventual", "evidence", "exchange", "exciting", "exercise",
        "explicit", "explorer", "exponent", "external", "fabulous", "facebook", "familiar",
        "families", "fanciest", "fantasia", "farewell", "favorite", "fearless", "feedback",
        "festival", "filename", "filament", "filtered", "fireside", "firmware", "flagship",
        "flamingo", "floating", "folklore", "footwear", "forecast", "forehead", "forensic",
        "fortress", "fraction", "fragment", "freckles", "freehand", "friendly", "frontier"
    ];

    public ScrambleGame StartScramble(ulong channelId)
    {
        var word = ScrambleWords[_rng.Next(ScrambleWords.Length)];
        var scrambled = ScrambleWord(word);

        var game = new ScrambleGame
        {
            OriginalWord = word,
            ScrambledWord = scrambled
        };

        _scrambleGames[channelId] = game;
        return game;
    }

    public (bool correct, ScrambleGame game) CheckScramble(ulong channelId, string answer)
    {
        if (!_scrambleGames.TryGetValue(channelId, out var game))
            return (false, null);

        if (string.Equals(answer.Trim(), game.OriginalWord, StringComparison.OrdinalIgnoreCase))
        {
            _scrambleGames.TryRemove(channelId, out _);
            return (true, game);
        }

        return (false, game);
    }

    public ScrambleGame GetScrambleGame(ulong channelId)
        => _scrambleGames.TryGetValue(channelId, out var g) ? g : null;

    public void EndScramble(ulong channelId)
        => _scrambleGames.TryRemove(channelId, out _);

    private string ScrambleWord(string word)
    {
        var chars = word.ToCharArray();
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        // Make sure it's actually different
        var scrambled = new string(chars);
        if (scrambled == word && word.Length > 1)
        {
            (chars[0], chars[1]) = (chars[1], chars[0]);
            scrambled = new string(chars);
        }

        return scrambled;
    }

    // ═══════════════════════════════════════════
    //  GEOGRAPHY QUIZ
    // ═══════════════════════════════════════════

    private static readonly Dictionary<string, string> CountryCapitals = new()
    {
        ["Afghanistan"] = "Kabul",
        ["Albania"] = "Tirana",
        ["Algeria"] = "Algiers",
        ["Argentina"] = "Buenos Aires",
        ["Australia"] = "Canberra",
        ["Austria"] = "Vienna",
        ["Bangladesh"] = "Dhaka",
        ["Belgium"] = "Brussels",
        ["Bolivia"] = "La Paz",
        ["Brazil"] = "Brasilia",
        ["Bulgaria"] = "Sofia",
        ["Cambodia"] = "Phnom Penh",
        ["Canada"] = "Ottawa",
        ["Chile"] = "Santiago",
        ["China"] = "Beijing",
        ["Colombia"] = "Bogota",
        ["Costa Rica"] = "San Jose",
        ["Croatia"] = "Zagreb",
        ["Cuba"] = "Havana",
        ["Czech Republic"] = "Prague",
        ["Denmark"] = "Copenhagen",
        ["Dominican Republic"] = "Santo Domingo",
        ["Ecuador"] = "Quito",
        ["Egypt"] = "Cairo",
        ["El Salvador"] = "San Salvador",
        ["Estonia"] = "Tallinn",
        ["Ethiopia"] = "Addis Ababa",
        ["Finland"] = "Helsinki",
        ["France"] = "Paris",
        ["Germany"] = "Berlin",
        ["Greece"] = "Athens",
        ["Guatemala"] = "Guatemala City",
        ["Honduras"] = "Tegucigalpa",
        ["Hungary"] = "Budapest",
        ["Iceland"] = "Reykjavik",
        ["India"] = "New Delhi",
        ["Indonesia"] = "Jakarta",
        ["Iran"] = "Tehran",
        ["Iraq"] = "Baghdad",
        ["Ireland"] = "Dublin",
        ["Israel"] = "Jerusalem",
        ["Italy"] = "Rome",
        ["Jamaica"] = "Kingston",
        ["Japan"] = "Tokyo",
        ["Jordan"] = "Amman",
        ["Kenya"] = "Nairobi",
        ["Latvia"] = "Riga",
        ["Lebanon"] = "Beirut",
        ["Lithuania"] = "Vilnius",
        ["Malaysia"] = "Kuala Lumpur",
        ["Mexico"] = "Mexico City",
        ["Mongolia"] = "Ulaanbaatar",
        ["Morocco"] = "Rabat",
        ["Nepal"] = "Kathmandu",
        ["Netherlands"] = "Amsterdam",
        ["New Zealand"] = "Wellington",
        ["Nigeria"] = "Abuja",
        ["North Korea"] = "Pyongyang",
        ["Norway"] = "Oslo",
        ["Pakistan"] = "Islamabad",
        ["Panama"] = "Panama City",
        ["Paraguay"] = "Asuncion",
        ["Peru"] = "Lima",
        ["Philippines"] = "Manila",
        ["Poland"] = "Warsaw",
        ["Portugal"] = "Lisbon",
        ["Romania"] = "Bucharest",
        ["Russia"] = "Moscow",
        ["Saudi Arabia"] = "Riyadh",
        ["Serbia"] = "Belgrade",
        ["Singapore"] = "Singapore",
        ["Slovakia"] = "Bratislava",
        ["Slovenia"] = "Ljubljana",
        ["South Africa"] = "Pretoria",
        ["South Korea"] = "Seoul",
        ["Spain"] = "Madrid",
        ["Sri Lanka"] = "Colombo",
        ["Sweden"] = "Stockholm",
        ["Switzerland"] = "Bern",
        ["Syria"] = "Damascus",
        ["Taiwan"] = "Taipei",
        ["Thailand"] = "Bangkok",
        ["Tunisia"] = "Tunis",
        ["Turkey"] = "Ankara",
        ["Ukraine"] = "Kyiv",
        ["United Kingdom"] = "London",
        ["United States"] = "Washington D.C.",
        ["Uruguay"] = "Montevideo",
        ["Venezuela"] = "Caracas",
        ["Vietnam"] = "Hanoi",
        ["Zimbabwe"] = "Harare"
    };

    public (string country, string capital) GetGeoQuestion()
    {
        var keys = CountryCapitals.Keys.ToList();
        var country = keys[_rng.Next(keys.Count)];
        return (country, CountryCapitals[country]);
    }

    public static bool CheckGeoAnswer(string answer, string correctCapital)
        => string.Equals(answer.Trim(), correctCapital, StringComparison.OrdinalIgnoreCase);

    // ═══════════════════════════════════════════
    //  MATH RACE
    // ═══════════════════════════════════════════

    public (string problem, int answer) GenerateMathProblem()
    {
        var op = _rng.Next(3);
        int a, b, answer;
        string symbol;

        switch (op)
        {
            case 0: // addition
                a = _rng.Next(10, 500);
                b = _rng.Next(10, 500);
                answer = a + b;
                symbol = "+";
                break;
            case 1: // subtraction
                a = _rng.Next(50, 500);
                b = _rng.Next(10, a);
                answer = a - b;
                symbol = "-";
                break;
            default: // multiplication
                a = _rng.Next(2, 25);
                b = _rng.Next(2, 25);
                answer = a * b;
                symbol = "\u00d7";
                break;
        }

        return ($"{a} {symbol} {b}", answer);
    }

    // ═══════════════════════════════════════════
    //  EMOJI QUIZ
    // ═══════════════════════════════════════════

    private static readonly (string emojis, string answer)[] EmojiQuizData =
    [
        ("\ud83e\udd81\ud83d\udc51", "The Lion King"),
        ("\ud83d\udc7d\ud83d\ude80\u2b50", "Star Wars"),
        ("\ud83d\udd77\ufe0f\ud83e\uddb8", "Spider-Man"),
        ("\ud83e\uddf9\u2728\ud83c\udff0", "Harry Potter"),
        ("\ud83e\udd96\ud83c\udfd4\ufe0f", "Jurassic Park"),
        ("\u2744\ufe0f\ud83d\udc78\u2603\ufe0f", "Frozen"),
        ("\ud83c\udfb5\ud83c\udfb6\ud83c\udfb5\ud83c\udfa4", "Pitch Perfect"),
        ("\ud83d\udc8d\ud83c\udf0b\ud83e\uddd9", "Lord of the Rings"),
        ("\ud83d\udc20\ud83c\udf0a\ud83d\udd0d", "Finding Nemo"),
        ("\ud83e\uddf8\ud83d\ude80\ud83c\udf19", "Toy Story"),
        ("\ud83d\udc7b\ud83d\ude31\ud83d\udcde", "Ghostbusters"),
        ("\ud83e\udddf\ud83e\ude78\ud83c\udf19", "Twilight"),
        ("\ud83d\udc80\ud83c\udff4\u200d\u2620\ufe0f\ud83d\udea2", "Pirates of the Caribbean"),
        ("\ud83c\udfce\ufe0f\u26a1\ud83c\udfc1", "Cars"),
        ("\ud83e\udd16\u2764\ufe0f\ud83c\udf31", "Wall-E"),
        ("\ud83e\udd20\ud83d\udc0d\ud83c\udfdb\ufe0f", "Indiana Jones"),
        ("\ud83e\udddb\ud83e\ude78\ud83c\udf19", "Dracula"),
        ("\ud83d\udc12\ud83c\udf34\ud83d\udcd6", "Tarzan"),
        ("\ud83e\uddb8\ud83d\udca5\ud83e\udd1c\ud83e\udd1b", "The Avengers"),
        ("\ud83c\udf0a\ud83d\udea2\u2764\ufe0f", "Titanic"),
        ("\ud83d\udc80\ud83c\udfad\ud83c\udfb6", "Phantom of the Opera"),
        ("\ud83d\udc2d\ud83c\udf73\ud83c\uddf2\ud83c\uddeb", "Ratatouille"),
        ("\ud83e\uddd1\u200d\ud83d\ude80\ud83c\udf11\ud83d\udc63", "First Man"),
        ("\ud83c\udfc8\ud83d\udc68\u200d\ud83d\udcbc\ud83c\udfc6", "The Blind Side"),
        ("\ud83e\uddb4\u2694\ufe0f\ud83c\udff0", "Shrek"),
        ("\ud83d\udc3b\u2744\ufe0f\ud83c\udf32", "Brave"),
        ("\ud83c\udf0d\ud83d\udc80\ud83e\udddf", "World War Z"),
        ("\ud83c\udfa9\u2728\ud83d\udc07", "The Prestige"),
        ("\ud83c\udfb9\ud83c\udfa4\u2b50", "Bohemian Rhapsody"),
        ("\ud83e\uddd9\u200d\u2642\ufe0f\ud83d\udd2e\u2728", "Doctor Strange"),
        ("\ud83d\ude97\ud83d\udca8\ud83d\udcb0", "Baby Driver"),
        ("\ud83d\udc68\u200d\ud83d\udc67\ud83c\udfdd\ufe0f\ud83d\udc1f", "Finding Dory"),
        ("\ud83e\udd35\ud83c\udfb0\ud83c\udf78", "Casino Royale"),
        ("\ud83d\udc51\ud83e\udd81\ud83c\udf0d", "Black Panther"),
        ("\ud83d\udc68\u200d\ud83c\udfa4\ud83c\udfb5\u2764\ufe0f", "A Star Is Born")
    ];

    public (string emojis, string answer) GetEmojiQuiz()
    {
        var entry = EmojiQuizData[_rng.Next(EmojiQuizData.Length)];
        return entry;
    }

    public static bool CheckEmojiQuizAnswer(string guess, string correctAnswer)
    {
        var g = guess.Trim().ToLowerInvariant()
            .Replace("the ", "").Replace("a ", "").Replace("an ", "");
        var c = correctAnswer.ToLowerInvariant()
            .Replace("the ", "").Replace("a ", "").Replace("an ", "");

        return string.Equals(g, c, StringComparison.OrdinalIgnoreCase)
               || c.Contains(g, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════
    //  MEMORY GAME
    // ═══════════════════════════════════════════

    public sealed class MemoryGameState
    {
        public List<string> Sequence { get; init; } = new();
        public int Level { get; set; } = 1;
        public int Score { get; set; }
    }

    private static readonly string[] MemoryEmojis =
    [
        "\ud83c\udf4e", "\ud83c\udf4a", "\ud83c\udf4b", "\ud83c\udf49",
        "\ud83c\udf47", "\ud83c\udf53", "\ud83e\udd5d", "\ud83c\udf52",
        "\ud83c\udf51", "\ud83e\udd6d", "\ud83c\udf4c", "\ud83c\udf50",
        "\u2b50", "\ud83c\udf19", "\u2764\ufe0f", "\ud83d\udd25",
        "\ud83c\udf0a", "\ud83c\udf3a", "\ud83c\udf3b", "\ud83c\udf3c"
    ];

    public MemoryGameState StartMemoryGame(ulong channelId)
    {
        var game = new MemoryGameState { Level = 1 };
        GenerateMemorySequence(game);
        _memoryGames[channelId] = game;
        return game;
    }

    private void GenerateMemorySequence(MemoryGameState game)
    {
        game.Sequence.Clear();
        var count = game.Level + 2; // start with 3 emojis, increase each level
        for (var i = 0; i < count; i++)
            game.Sequence.Add(MemoryEmojis[_rng.Next(MemoryEmojis.Length)]);
    }

    public (bool correct, MemoryGameState game) CheckMemoryAnswer(ulong channelId, string answer)
    {
        if (!_memoryGames.TryGetValue(channelId, out var game))
            return (false, null);

        var parts = answer.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != game.Sequence.Count)
        {
            _memoryGames.TryRemove(channelId, out _);
            return (false, game);
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim() != game.Sequence[i])
            {
                _memoryGames.TryRemove(channelId, out _);
                return (false, game);
            }
        }

        // Correct! Level up
        game.Score++;
        game.Level++;
        GenerateMemorySequence(game);
        return (true, game);
    }

    public MemoryGameState GetMemoryGame(ulong channelId)
        => _memoryGames.TryGetValue(channelId, out var g) ? g : null;

    public void EndMemoryGame(ulong channelId)
        => _memoryGames.TryRemove(channelId, out _);
}
