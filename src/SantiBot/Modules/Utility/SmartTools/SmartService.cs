#nullable disable
using Santi.Common;

namespace SantiBot.Modules.Utility.SmartTools;

public sealed class SmartService : INService
{
    private readonly SantiRandom _rng;

    // Guild FAQ storage: guildId -> (question -> answer)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong,
        System.Collections.Concurrent.ConcurrentDictionary<string, string>> _faqStore = new();

    public SmartService()
    {
        _rng = new SantiRandom();
    }

    // ──────────────────────────────────────────────
    //  1. CONVERSATION SUMMARIZER
    // ──────────────────────────────────────────────

    /// <summary>
    /// Takes a list of messages and produces a bullet-point summary
    /// using extractive logic: picks sentences with the most keyword overlap.
    /// </summary>
    public string SummarizeMessages(IReadOnlyList<string> messages, int bulletCount = 5)
    {
        if (messages is null || messages.Count == 0)
            return "No messages to summarize.";

        // Split all messages into individual sentences
        var sentences = new List<string>();
        foreach (var msg in messages)
        {
            var parts = msg.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                if (p.Length >= 8) // skip tiny fragments
                    sentences.Add(p.Trim());
            }
        }

        if (sentences.Count == 0)
            return "Messages were too short to summarize.";

        // Build global keyword frequency (skip stop words)
        var wordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var sentence in sentences)
        {
            foreach (var word in ExtractWords(sentence))
            {
                if (!_stopWords.Contains(word.ToLowerInvariant()) && word.Length > 2)
                    wordFreq[word.ToLowerInvariant()] = wordFreq.GetValueOrDefault(word.ToLowerInvariant()) + 1;
            }
        }

        // Score each sentence by sum of its word frequencies
        var scored = sentences
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(s => (Sentence: s, Score: ExtractWords(s)
                .Where(w => w.Length > 2 && !_stopWords.Contains(w.ToLowerInvariant()))
                .Sum(w => wordFreq.GetValueOrDefault(w.ToLowerInvariant()))))
            .OrderByDescending(x => x.Score)
            .Take(Math.Min(bulletCount, sentences.Count))
            .ToList();

        if (scored.Count == 0)
            return "Could not extract key points.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("**Conversation Summary:**");
        foreach (var (sentence, _) in scored)
            sb.AppendLine($"- {sentence}");

        return sb.ToString().TrimEnd();
    }

    private static string[] ExtractWords(string text)
        => text.Split(new[] { ' ', ',', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "shall",
        "should", "may", "might", "must", "can", "could", "i", "me", "my",
        "we", "our", "you", "your", "he", "him", "his", "she", "her", "it",
        "its", "they", "them", "their", "this", "that", "these", "those",
        "and", "but", "or", "nor", "not", "so", "yet", "both", "either",
        "neither", "for", "to", "from", "in", "on", "at", "by", "with",
        "about", "of", "up", "out", "off", "over", "into", "just", "also",
        "than", "then", "if", "when", "what", "which", "who", "how", "all",
        "each", "every", "any", "few", "more", "most", "some", "such", "no",
        "only", "very", "too", "here", "there", "where", "now", "get", "got"
    };

    // ──────────────────────────────────────────────
    //  2. SMART FAQ
    // ──────────────────────────────────────────────

    public void AddFaq(ulong guildId, string question, string answer)
    {
        var guildFaqs = _faqStore.GetOrAdd(guildId, _ => new());
        guildFaqs[question.ToLowerInvariant().Trim()] = answer;
    }

    public bool RemoveFaq(ulong guildId, string question)
    {
        if (!_faqStore.TryGetValue(guildId, out var guildFaqs))
            return false;
        return guildFaqs.TryRemove(question.ToLowerInvariant().Trim(), out _);
    }

    public IReadOnlyList<(string Question, string Answer)> GetAllFaqs(ulong guildId)
    {
        if (!_faqStore.TryGetValue(guildId, out var guildFaqs))
            return Array.Empty<(string, string)>();
        return guildFaqs.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>
    /// Finds the best FAQ match for an incoming question using keyword similarity.
    /// Returns null if no good match is found (threshold: 40% keyword overlap).
    /// </summary>
    public (string Question, string Answer)? FindAnswer(ulong guildId, string input)
    {
        if (!_faqStore.TryGetValue(guildId, out var guildFaqs) || guildFaqs.IsEmpty)
            return null;

        var inputWords = ExtractWords(input.ToLowerInvariant())
            .Where(w => !_stopWords.Contains(w) && w.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (inputWords.Count == 0)
            return null;

        (string Question, string Answer)? best = null;
        double bestScore = 0;

        foreach (var kv in guildFaqs)
        {
            var faqWords = ExtractWords(kv.Key)
                .Where(w => !_stopWords.Contains(w) && w.Length > 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (faqWords.Count == 0)
                continue;

            var overlap = inputWords.Intersect(faqWords).Count();
            var score = (double)overlap / Math.Max(inputWords.Count, faqWords.Count);

            if (score > bestScore)
            {
                bestScore = score;
                best = (kv.Key, kv.Value);
            }
        }

        // Require at least 40% keyword overlap
        return bestScore >= 0.4 ? best : null;
    }

    // ──────────────────────────────────────────────
    //  3. SENTIMENT ANALYZER
    // ──────────────────────────────────────────────

    public (int Score, string Label) AnalyzeSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, "Neutral");

        var words = ExtractWords(text.ToLowerInvariant());
        int positive = 0;
        int negative = 0;

        foreach (var word in words)
        {
            if (_positiveWords.Contains(word)) positive++;
            if (_negativeWords.Contains(word)) negative++;
        }

        int total = positive + negative;
        if (total == 0)
            return (0, "Neutral");

        // Score from -100 to +100
        int score = (int)Math.Round(((double)(positive - negative) / total) * 100.0);
        score = Math.Clamp(score, -100, 100);

        var label = score switch
        {
            <= -60 => "Very Negative",
            <= -20 => "Negative",
            < 20 => "Neutral",
            < 60 => "Positive",
            _ => "Very Positive"
        };

        return (score, label);
    }

    private static readonly HashSet<string> _positiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "good", "great", "awesome", "amazing", "wonderful", "fantastic", "excellent",
        "love", "happy", "joy", "beautiful", "brilliant", "perfect", "nice", "best",
        "fun", "enjoy", "delightful", "superb", "outstanding", "incredible", "marvelous",
        "pleasant", "terrific", "splendid", "magnificent", "glorious", "fabulous",
        "lovely", "charming", "cheerful", "grateful", "thankful", "blessed", "excited",
        "thrilled", "ecstatic", "elated", "overjoyed", "pleased", "satisfied", "proud",
        "confident", "hopeful", "inspired", "motivated", "passionate", "peaceful",
        "calm", "relaxed", "comfortable", "warm", "kind", "generous", "brave",
        "strong", "success", "win", "laugh", "smile", "celebrate", "triumph",
        "paradise", "heaven", "miracle", "treasure", "genius", "legend", "epic"
    };

    private static readonly HashSet<string> _negativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad", "terrible", "awful", "horrible", "worst", "hate", "angry", "sad",
        "ugly", "stupid", "boring", "disgusting", "dreadful", "pathetic", "miserable",
        "annoying", "frustrating", "disappointing", "depressing", "painful", "toxic",
        "nasty", "cruel", "rude", "mean", "selfish", "lazy", "weak", "fail",
        "disaster", "tragedy", "nightmare", "garbage", "trash", "worthless", "useless",
        "hopeless", "helpless", "broken", "ruined", "destroyed", "wrecked", "doomed",
        "furious", "enraged", "livid", "bitter", "jealous", "anxious", "stressed",
        "worried", "scared", "afraid", "terrified", "lonely", "heartbroken", "grief",
        "sorrow", "agony", "torment", "suffering", "abuse", "scam", "fraud",
        "lie", "cheat", "betray", "abandon", "reject", "insult", "mock"
    };

    // ──────────────────────────────────────────────
    //  4. TOPIC DETECTOR
    // ──────────────────────────────────────────────

    public IReadOnlyList<(string Topic, int Hits)> DetectTopics(string text, int topN = 3)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<(string, int)>();

        var words = ExtractWords(text.ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scores = new List<(string Topic, int Hits)>();
        foreach (var (topic, keywords) in _topicKeywords)
        {
            var hits = keywords.Count(w => words.Contains(w));
            if (hits > 0)
                scores.Add((topic, hits));
        }

        return scores
            .OrderByDescending(x => x.Hits)
            .Take(topN)
            .ToList();
    }

    private static readonly Dictionary<string, string[]> _topicKeywords = new()
    {
        ["Gaming"] = new[]
        {
            "game", "games", "gaming", "play", "player", "gamer", "console", "pc",
            "xbox", "playstation", "nintendo", "switch", "steam", "fps", "rpg", "mmo",
            "fortnite", "minecraft", "valorant", "league", "apex", "cod", "warzone",
            "controller", "keyboard", "mouse", "stream", "twitch", "esports", "ranked",
            "level", "boss", "loot", "quest", "raid", "dungeon", "pvp", "pve"
        },
        ["Music"] = new[]
        {
            "music", "song", "songs", "album", "artist", "band", "guitar", "piano",
            "drums", "bass", "vocal", "singer", "concert", "tour", "playlist", "spotify",
            "rap", "hiphop", "rock", "pop", "jazz", "metal", "edm", "beat", "melody",
            "lyric", "lyrics", "producer", "remix", "vinyl", "headphones", "audio"
        },
        ["Art"] = new[]
        {
            "art", "draw", "drawing", "paint", "painting", "sketch", "digital",
            "canvas", "brush", "color", "colour", "design", "illustration", "artist",
            "gallery", "sculpture", "creative", "aesthetic", "portrait", "watercolor",
            "commission", "fanart", "doodle", "ink", "pencil", "photoshop", "procreate"
        },
        ["Tech"] = new[]
        {
            "tech", "technology", "code", "coding", "programming", "software", "hardware",
            "computer", "laptop", "phone", "app", "developer", "engineer", "python",
            "javascript", "api", "database", "server", "cloud", "ai", "machine",
            "algorithm", "debug", "deploy", "github", "linux", "windows", "mac",
            "cpu", "gpu", "ram", "ssd", "wifi", "bluetooth", "internet", "browser"
        },
        ["Sports"] = new[]
        {
            "sports", "sport", "football", "soccer", "basketball", "baseball", "hockey",
            "tennis", "golf", "swim", "run", "marathon", "gym", "workout", "fitness",
            "athlete", "team", "coach", "score", "goal", "touchdown", "championship",
            "tournament", "league", "nba", "nfl", "mlb", "nhl", "fifa", "olympics"
        },
        ["Food"] = new[]
        {
            "food", "eat", "eating", "cook", "cooking", "recipe", "meal", "dinner",
            "lunch", "breakfast", "snack", "pizza", "burger", "sushi", "pasta", "rice",
            "chicken", "beef", "vegan", "vegetarian", "dessert", "cake", "chocolate",
            "coffee", "tea", "restaurant", "kitchen", "chef", "bake", "baking", "grill"
        },
        ["School"] = new[]
        {
            "school", "class", "homework", "study", "exam", "test", "grade", "teacher",
            "professor", "college", "university", "student", "lecture", "assignment",
            "essay", "math", "science", "history", "english", "biology", "chemistry",
            "physics", "degree", "graduation", "campus", "textbook", "library", "tutor"
        },
        ["Movies"] = new[]
        {
            "movie", "movies", "film", "films", "cinema", "theater", "actor", "actress",
            "director", "scene", "plot", "trailer", "sequel", "prequel", "series",
            "netflix", "disney", "marvel", "dc", "horror", "comedy", "drama", "action",
            "thriller", "documentary", "oscar", "popcorn", "screenplay", "blockbuster"
        },
        ["Anime"] = new[]
        {
            "anime", "manga", "otaku", "waifu", "weeb", "naruto", "onepiece", "dragonball",
            "attack", "titan", "demon", "slayer", "jujutsu", "kaisen", "studio", "ghibli",
            "sub", "dub", "cosplay", "isekai", "shonen", "seinen", "weeaboo", "senpai",
            "kawaii", "chibi", "harem", "mecha", "slice", "life"
        },
        ["Politics"] = new[]
        {
            "politics", "political", "government", "president", "election", "vote",
            "democrat", "republican", "congress", "senate", "law", "policy", "tax",
            "economy", "healthcare", "immigration", "rights", "freedom", "justice",
            "protest", "liberal", "conservative", "debate", "campaign", "legislation",
            "regulation", "corruption", "democracy", "referendum"
        },
        ["Memes"] = new[]
        {
            "meme", "memes", "lol", "lmao", "rofl", "bruh", "sus", "based", "cringe",
            "ratio", "cope", "seethe", "chad", "sigma", "gigachad", "npc", "shitpost",
            "dank", "cursed", "blessed", "wholesome", "oof", "yeet", "vibe", "poggers",
            "pepega", "monkas", "kekw", "deadass", "cap", "nocap", "bussin", "slay"
        }
    };

    // ──────────────────────────────────────────────
    //  5. SMART GREETING
    // ──────────────────────────────────────────────

    public string GenerateGreeting(DateTimeOffset? now = null)
    {
        var dt = now ?? DateTimeOffset.UtcNow;
        var hour = dt.Hour;
        var day = dt.DayOfWeek;

        // Time-of-day greeting
        var timeGreeting = hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 17 => "Good afternoon",
            >= 17 and < 21 => "Good evening",
            _ => "Good night"
        };

        // Day-specific flair
        var dayFlair = day switch
        {
            DayOfWeek.Monday => "Hope your Monday is off to a great start!",
            DayOfWeek.Tuesday => "Taco Tuesday vibes!",
            DayOfWeek.Wednesday => "Happy Hump Day! We're halfway there!",
            DayOfWeek.Thursday => "Almost Friday, hang in there!",
            DayOfWeek.Friday => "Happy Friday! The weekend is here!",
            DayOfWeek.Saturday => "Happy Saturday! Enjoy your weekend!",
            DayOfWeek.Sunday => "Happy Sunday! Rest up and recharge!",
            _ => "Have an awesome day!"
        };

        // Fun weather (simulated)
        var weatherOptions = new[]
        {
            "Looks like a perfect day for some Discord chatting!",
            "Whether it's sunny or rainy outside, it's always fun in here!",
            "Hope the weather is treating you well today!",
            "Perfect weather for gaming and hanging out!",
            "Cloudy with a chance of great conversations!",
            "The forecast says: 100% chance of fun!",
            "Sunny skies in the chat today!",
            "Rain or shine, this server is the place to be!"
        };

        var weather = weatherOptions[_rng.Next(0, weatherOptions.Length)];

        return $"{timeGreeting}! {dayFlair} {weather}";
    }

    // ──────────────────────────────────────────────
    //  6. WRITING HELPER
    // ──────────────────────────────────────────────

    public WritingReport CheckWriting(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new WritingReport { OriginalText = text ?? "", CorrectedText = "", Issues = new() };

        var corrected = text;
        var issues = new List<string>();

        // Fix common typos
        foreach (var (typo, fix) in _commonTypos)
        {
            if (corrected.Contains(typo, StringComparison.OrdinalIgnoreCase))
            {
                corrected = System.Text.RegularExpressions.Regex.Replace(
                    corrected, $@"\b{System.Text.RegularExpressions.Regex.Escape(typo)}\b",
                    fix, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                issues.Add($"Typo: '{typo}' -> '{fix}'");
            }
        }

        // Check sentence length
        var sentences = corrected.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var sentence in sentences)
        {
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 35)
                issues.Add($"Long sentence ({wordCount} words): \"{(sentence.Length > 60 ? sentence[..60] + "..." : sentence)}\"");
        }

        // Check for passive voice indicators
        var passiveCount = 0;
        foreach (var indicator in _passiveIndicators)
        {
            if (corrected.Contains(indicator, StringComparison.OrdinalIgnoreCase))
                passiveCount++;
        }

        if (passiveCount > 0)
            issues.Add($"Possible passive voice detected ({passiveCount} indicator{(passiveCount > 1 ? "s" : "")})");

        // Word count and reading level estimate
        var totalWords = corrected.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var avgWordLength = corrected.Replace(" ", "").Length / (double)Math.Max(totalWords, 1);

        return new WritingReport
        {
            OriginalText = text,
            CorrectedText = corrected,
            Issues = issues,
            WordCount = totalWords,
            SentenceCount = sentences.Length,
            AvgWordLength = Math.Round(avgWordLength, 1),
            TyposFixed = issues.Count(i => i.StartsWith("Typo:"))
        };
    }

    private static readonly (string Typo, string Fix)[] _commonTypos = new[]
    {
        ("teh", "the"), ("recieve", "receive"), ("seperate", "separate"),
        ("definately", "definitely"), ("occured", "occurred"), ("untill", "until"),
        ("thier", "their"), ("wierd", "weird"), ("alot", "a lot"),
        ("arguement", "argument"), ("begining", "beginning"), ("beleive", "believe"),
        ("calender", "calendar"), ("cemetary", "cemetery"), ("collegue", "colleague"),
        ("comittee", "committee"), ("concious", "conscious"), ("curiousity", "curiosity"),
        ("embarass", "embarrass"), ("enviroment", "environment"), ("exersize", "exercise"),
        ("existance", "existence"), ("foriegn", "foreign"), ("goverment", "government"),
        ("gaurd", "guard"), ("happend", "happened"), ("harrass", "harass"),
        ("immediatly", "immediately"), ("independant", "independent"), ("knowlege", "knowledge"),
        ("liason", "liaison"), ("manuever", "maneuver"), ("millenium", "millennium"),
        ("neccessary", "necessary"), ("noticable", "noticeable"), ("occassion", "occasion"),
        ("parliment", "parliament"), ("perseverence", "perseverance"), ("posession", "possession"),
        ("privelege", "privilege"), ("publically", "publicly"), ("recomend", "recommend"),
        ("refrence", "reference"), ("relevent", "relevant"), ("rythm", "rhythm"),
        ("succesful", "successful"), ("suprise", "surprise"), ("tommorow", "tomorrow"),
        ("truely", "truly"), ("unforseen", "unforeseen"), ("vaccuum", "vacuum"),
        ("wether", "whether"), ("writting", "writing"), ("wich", "which"),
        ("becuase", "because"), ("accomodate", "accommodate"), ("acheive", "achieve"),
        ("acquaintence", "acquaintance"), ("adress", "address"), ("agressive", "aggressive")
    };

    private static readonly string[] _passiveIndicators = new[]
    {
        "was done", "was made", "was taken", "was given", "was found",
        "was created", "was built", "was written", "was sent", "was broken",
        "were done", "were made", "were taken", "were given", "were found",
        "is being", "are being", "has been", "have been", "had been",
        "will be done", "will be made", "will be taken", "being done",
        "being made", "being taken", "been done", "been made", "been taken"
    };

    // ──────────────────────────────────────────────
    //  7. NAME GENERATOR
    // ──────────────────────────────────────────────

    public string GenerateName(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "character" => CombineName(_characterPrefixes, _characterSuffixes),
            "band" => CombineName(_bandPrefixes, _bandSuffixes),
            "superhero" => CombineName(_superheroPrefixes, _superheroSuffixes),
            "fantasy" => CombineName(_fantasyPrefixes, _fantasySuffixes),
            "elf" => CombineName(_elfPrefixes, _elfSuffixes),
            "dwarf" => CombineName(_dwarfPrefixes, _dwarfSuffixes),
            "orc" => CombineName(_orcPrefixes, _orcSuffixes),
            _ => CombineName(_fantasyPrefixes, _fantasySuffixes)
        };
    }

    public static readonly string[] NameTypes = { "character", "band", "superhero", "fantasy", "elf", "dwarf", "orc" };

    private string CombineName(string[] prefixes, string[] suffixes)
        => prefixes[_rng.Next(0, prefixes.Length)] + suffixes[_rng.Next(0, suffixes.Length)];

    private static readonly string[] _characterPrefixes = new[]
    {
        "Alex", "Blake", "Carter", "Drake", "Ellis", "Finn", "Gray",
        "Hunter", "Ivy", "Jade", "Knox", "Luna", "Miles", "Nova",
        "Owen", "Piper", "Quinn", "Raven", "Sage", "Tyler",
        "Ace", "Blaze", "Cleo", "Dash", "Echo"
    };

    private static readonly string[] _characterSuffixes = new[]
    {
        " Stone", " Wolf", " Frost", " Blaze", " Storm", " Vale",
        " Cross", " Steel", " Knight", " Fox", " Rivers", " West",
        " Black", " Ryder", " Brooks", " Hart", " Pierce", " Wilde",
        " Thorne", " Drake", " Moon", " Ash", " Cole", " Reed"
    };

    private static readonly string[] _bandPrefixes = new[]
    {
        "The Crimson", "Electric", "Midnight", "Neon", "Phantom",
        "Velvet", "Atomic", "Crystal", "Dark", "Frozen",
        "Golden", "Hollow", "Iron", "Lunar", "Mystic",
        "Nuclear", "Primal", "Rebel", "Silent", "Twisted",
        "Cosmic", "Savage", "Broken", "Burning", "Digital"
    };

    private static readonly string[] _bandSuffixes = new[]
    {
        " Wolves", " Dragons", " Echoes", " Horizons", " Shadows",
        " Flames", " Roses", " Serpents", " Thunder", " Vipers",
        " Foxes", " Ravens", " Phantoms", " Sirens", " Titans",
        " Outlaws", " Rebels", " Specters", " Tempest", " Vortex",
        " Circuit", " Prophecy", " Alchemy", " Cascade"
    };

    private static readonly string[] _superheroPrefixes = new[]
    {
        "Captain", "Doctor", "Shadow", "Thunder", "Iron",
        "Silver", "Crimson", "Hyper", "Ultra", "Mega",
        "Phantom", "Blaze", "Frost", "Storm", "Quantum",
        "Cosmic", "Atomic", "Turbo", "Power", "Dark",
        "Star", "Solar", "Night", "Cyber", "Nova"
    };

    private static readonly string[] _superheroSuffixes = new[]
    {
        " Bolt", " Fist", " Wing", " Strike", " Shield",
        " Blaze", " Hawk", " Storm", " Fury", " Ghost",
        " Knight", " Ranger", " Viper", " Blade", " Flash",
        " Titan", " Phoenix", " Sentinel", " Guardian", " Phantom",
        " Spark", " Force", " Wave", " Surge"
    };

    private static readonly string[] _fantasyPrefixes = new[]
    {
        "Ael", "Bran", "Cael", "Dor", "Eld", "Fen", "Gal",
        "Hal", "Ith", "Jor", "Kel", "Lor", "Mor", "Nar",
        "Oth", "Pael", "Quel", "Rath", "Syl", "Thal",
        "Ulr", "Val", "Wyr", "Xar", "Zeph"
    };

    private static readonly string[] _fantasySuffixes = new[]
    {
        "andor", "ith", "orn", "wyn", "ion", "ael", "eth",
        "ius", "oth", "ren", "mir", "zan", "dril", "gon",
        "thas", "vyn", "wick", "storm", "fire", "moon",
        "star", "blade", "heart", "wind"
    };

    private static readonly string[] _elfPrefixes = new[]
    {
        "Ael", "Alar", "Cel", "Dae", "Eil", "Fael", "Gal",
        "Hael", "Ith", "Lir", "Mae", "Nal", "Oel", "Phe",
        "Quel", "Rael", "Sil", "Tae", "Uel", "Vael",
        "Yl", "Zil", "Ari", "Ela", "Nym"
    };

    private static readonly string[] _elfSuffixes = new[]
    {
        "andil", "arion", "iel", "ithil", "wen", "dor", "las",
        "mir", "orn", "riel", "thien", "wyn", "ael", "eth",
        "ion", "orin", "uel", "yth", "andir", "endil",
        "indel", "ondil", "urel", "yneth"
    };

    private static readonly string[] _dwarfPrefixes = new[]
    {
        "Bal", "Brom", "Dag", "Dur", "Farg", "Gim", "Grun",
        "Hald", "Kaz", "Krag", "Mur", "Nor", "Rag", "Rud",
        "Skar", "Thor", "Thur", "Tor", "Uld", "Vog",
        "Bor", "Dral", "Grom", "Hurn", "Krond"
    };

    private static readonly string[] _dwarfSuffixes = new[]
    {
        "in", "dur", "grim", "nar", "rak", "din", "dak",
        "gard", "mund", "rim", "dor", "gar", "lin", "mir",
        "rik", "bold", "forge", "hammer", "stone", "iron",
        "axe", "helm", "shield", "beard"
    };

    private static readonly string[] _orcPrefixes = new[]
    {
        "Azg", "Borg", "Durg", "Gash", "Glob", "Grak", "Grub",
        "Gurz", "Krag", "Lug", "Mog", "Murg", "Naz", "Rag",
        "Shag", "Skul", "Snag", "Thrak", "Ugh", "Vrak",
        "Zug", "Grol", "Brug", "Durb", "Gorb"
    };

    private static readonly string[] _orcSuffixes = new[]
    {
        "ash", "gor", "gul", "mak", "nak", "rak", "shak",
        "tuk", "urg", "zag", "bash", "dak", "gash", "krag",
        "mash", "ruk", "skulk", "thud", "ugh", "warg",
        "fang", "rot", "bone", "blood"
    };

    // ──────────────────────────────────────────────
    //  Helper model for Writing Report
    // ──────────────────────────────────────────────

    public sealed class WritingReport
    {
        public string OriginalText { get; init; }
        public string CorrectedText { get; init; }
        public List<string> Issues { get; init; }
        public int WordCount { get; init; }
        public int SentenceCount { get; init; }
        public double AvgWordLength { get; init; }
        public int TyposFixed { get; init; }
    }
}
