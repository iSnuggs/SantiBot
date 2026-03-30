#nullable disable
using System.Collections.Concurrent;
using SantiBot.Modules.Utility.OpenClaw;

namespace SantiBot.Modules.Games.Dungeon;

/// <summary>
/// AI-powered dungeon narrator — calls OpenClaw for dramatic flavor text
/// and falls back to static templates when the AI is offline.
/// </summary>
public sealed class DungeonNarrator : INService
{
    private readonly OpenClawService _oc;
    private static readonly SantiRandom _rng = new();

    // Cache recent narrations so repeated scenarios don't spam the API.
    // Key = scenario hash, Value = (narration, timestamp)
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Text, DateTime Created)> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public DungeonNarrator(OpenClawService oc)
    {
        _oc = oc;
    }

    // ═══════════════════════════════════════════════════════════
    //  PUBLIC NARRATION METHODS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Atmospheric description of entering a new dungeon room.</summary>
    public async Task<string> NarrateRoomEntry(int roomNumber, int maxRooms, int difficulty, string partyClasses)
    {
        var cacheKey = $"room:{roomNumber}:{maxRooms}:{difficulty}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var prompt = "You are a dungeon master narrating a fantasy RPG in a Discord bot. " +
            $"In exactly 1-2 sentences, dramatically describe a party of adventurers ({partyClasses}) " +
            $"entering room {roomNumber} of {maxRooms} in a difficulty {difficulty} dungeon. " +
            "Be concise, atmospheric, and exciting. No markdown formatting.";

        var result = await TryAskAsync(prompt, cacheKey, "room");
        return result;
    }

    /// <summary>Dramatic description of a monster appearing.</summary>
    public async Task<string> NarrateMonsterEncounter(string monsterName, string monsterEmoji, int hp, int atk)
    {
        var cacheKey = $"monster:{monsterName}:{hp}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var prompt = "You are a dungeon master narrating a fantasy RPG in a Discord bot. " +
            $"In exactly 1-2 sentences, dramatically describe a {monsterName} {monsterEmoji} " +
            $"appearing before the party. It has {hp} HP and {atk} attack power. " +
            "Make it menacing and vivid. No markdown formatting.";

        var result = await TryAskAsync(prompt, cacheKey, "monster");
        return result;
    }

    /// <summary>Triumphant celebration of defeating a raid boss.</summary>
    public async Task<string> NarrateBossDefeat(string bossName, int participants, long totalDamage)
    {
        var cacheKey = $"boss:{bossName}:{participants}:{totalDamage}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var prompt = "You are a dungeon master narrating a fantasy RPG in a Discord bot. " +
            $"In exactly 2-3 sentences, write a triumphant celebration of {participants} heroes " +
            $"defeating the raid boss {bossName} after dealing {totalDamage:N0} total damage. " +
            "Make it epic and victorious. No markdown formatting.";

        var result = await TryAskAsync(prompt, cacheKey, "boss");
        return result;
    }

    /// <summary>Summary of completing an entire dungeon.</summary>
    public async Task<string> NarrateDungeonComplete(int difficulty, int roomsCleared, int monstersKilled, string className)
    {
        var cacheKey = $"complete:{difficulty}:{roomsCleared}:{monstersKilled}:{className}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var prompt = "You are a dungeon master narrating a fantasy RPG in a Discord bot. " +
            $"In exactly 2 sentences, summarize a {className} completing a difficulty {difficulty} dungeon " +
            $"after clearing {roomsCleared} rooms and slaying {monstersKilled} monsters. " +
            "Make it feel like a satisfying conclusion. No markdown formatting.";

        var result = await TryAskAsync(prompt, cacheKey, "complete");
        return result;
    }

    /// <summary>Dramatic death description for a fallen hero.</summary>
    public async Task<string> NarrateDeathScene(string playerName, string className, string monsterName)
    {
        var cacheKey = $"death:{playerName}:{className}:{monsterName}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var prompt = "You are a dungeon master narrating a fantasy RPG in a Discord bot. " +
            $"In exactly 1 sentence, dramatically describe {playerName} the {className} " +
            $"falling in battle against a {monsterName}. " +
            "Be dramatic but respectful. No markdown formatting.";

        var result = await TryAskAsync(prompt, cacheKey, "death");
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    //  STATIC FALLBACK TEMPLATES (usable without DI)
    // ═══════════════════════════════════════════════════════════

    private static readonly Dictionary<string, string[]> _fallbacks = new()
    {
        ["room"] = new[]
        {
            "The party pushes deeper into the dungeon, the air growing thick with ancient dust and the faint echo of something stirring ahead.",
            "Torchlight flickers across weathered stone walls as the adventurers step into the next chamber, weapons at the ready.",
            "A cold draft sweeps through the corridor as the party enters a new room — something about this place feels wrong.",
            "The heavy door groans open, revealing a shadowy chamber littered with the remains of those who came before.",
            "Bones crunch underfoot as the party advances into the darkness, the distant sound of dripping water their only companion.",
            "Ancient runes pulse faintly along the walls as the adventurers cross the threshold into uncharted depths.",
            "The stench of decay grows stronger as the party presses forward into yet another chamber of the forsaken dungeon.",
        },
        ["monster"] = new[]
        {
            "A terrible shriek fills the chamber as a monstrous figure lunges from the shadows, eyes burning with hunger!",
            "The ground trembles as a fearsome creature emerges from the darkness, its rage shaking dust from the ceiling!",
            "A guttural roar echoes off the stone walls — something ancient and furious has found the intruders!",
            "Claws scrape against stone as a nightmarish beast reveals itself, blocking the only path forward!",
            "The shadows coalesce into a terrifying form, and the adventurers realize they are not alone in this chamber!",
            "A pair of glowing eyes appear in the darkness, followed by the unmistakable sound of something very large and very angry.",
            "The torchlight catches a flash of fangs and scales as a creature lunges from its hiding place with murderous intent!",
        },
        ["boss"] = new[]
        {
            "With one final, earth-shaking blow, the mighty boss collapses! The heroes stand victorious amid the rubble, battered but unbroken. This tale will be sung in taverns for ages to come!",
            "The tyrant falls at last, its death cry echoing through every corridor of the dungeon! Against impossible odds, the raid party has triumphed. Tonight, they feast as legends!",
            "A blinding flash of light erupts as the boss lets out its final breath and crumbles to dust! The adventurers erupt in a war cry of victory — the nightmare is finally over!",
            "The ground shakes as the colossal foe crashes down for the last time! Cheers ring out across the battlefield as the heroes claim their hard-won glory!",
            "With a thunderous crash, the boss topples and the dungeon falls silent for the first time in centuries. The brave heroes have done what none thought possible. Victory is theirs!",
            "The beast's eyes dim as it slumps to the ground, defeated at last! The raiders collapse in exhaustion and relief — they have slain the unslayable!",
        },
        ["complete"] = new[]
        {
            "The dungeon has been conquered, every shadow purged and every monster slain. The hero emerges into daylight, forever changed by what they endured below.",
            "Against all odds, the adventurer has cleared every room and lived to tell the tale. The dungeon's treasures are theirs — hard-earned and well-deserved.",
            "The final chamber falls silent as the last foe crumbles. Bloodied but triumphant, the hero has proven their worth in the deepest dark.",
            "Every corridor explored, every beast vanquished — the dungeon is no more. The adventurer sheathes their weapon with the quiet satisfaction of a job well done.",
            "The dungeon's halls lie empty now, stripped of their terrors by a relentless warrior. Another chapter of legend written in sweat and steel.",
            "From the first room to the last, the adventurer carved a path through impossible danger. The dungeon bows to its new master.",
        },
        ["death"] = new[]
        {
            "The hero crumples to the ground, their light fading as the darkness of the dungeon claims another soul.",
            "A final gasp escapes their lips as the monster's blow lands true — the brave adventurer will rise no more.",
            "The dungeon floor runs red as another warrior falls, their quest ending not with glory, but with silence.",
            "With a sickening crunch, the hero is struck down, their journey cut short in the merciless depths below.",
            "The adventurer's eyes go dark as they collapse, adding their name to the long list of those the dungeon has devoured.",
            "One moment of hesitation is all it takes — the hero falls, and the dungeon's shadows swallow them whole.",
            "The monster roars in triumph as the fallen hero's weapon clatters uselessly across the cold stone floor.",
        },
    };

    /// <summary>
    /// Get a random fallback narration by type — callable without DI.
    /// Valid types: "room", "monster", "boss", "complete", "death"
    /// </summary>
    public static string GetFallback(string type)
    {
        if (!_fallbacks.TryGetValue(type, out var templates))
            return "The dungeon grows darker...";

        return templates[_rng.Next(templates.Length)];
    }

    // ═══════════════════════════════════════════════════════════
    //  INTERNALS
    // ═══════════════════════════════════════════════════════════

    private async Task<string> TryAskAsync(string prompt, string cacheKey, string fallbackType)
    {
        try
        {
            var (success, response) = await _oc.QuickAskAsync(prompt);

            if (success && !string.IsNullOrWhiteSpace(response))
            {
                // Trim to max 300 chars — keep it Discord-friendly
                var narration = response.Length > 300 ? response[..297] + "..." : response;
                CacheNarration(cacheKey, narration);
                return narration;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "DungeonNarrator AI call failed, using fallback");
        }

        // AI unavailable — use a static template
        var fallback = GetFallback(fallbackType);
        CacheNarration(cacheKey, fallback);
        return fallback;
    }

    private bool TryGetCached(string key, out string text)
    {
        // Prune expired entries occasionally (1 in 20 chance per lookup)
        if (_rng.Next(20) == 0)
            PruneCache();

        if (_cache.TryGetValue(key, out var entry) && DateTime.UtcNow - entry.Created < CacheTtl)
        {
            text = entry.Text;
            return true;
        }

        text = null;
        return false;
    }

    private void CacheNarration(string key, string text)
        => _cache[key] = (text, DateTime.UtcNow);

    private void PruneCache()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _cache)
        {
            if (now - kvp.Value.Created > CacheTtl)
                _cache.TryRemove(kvp.Key, out _);
        }
    }
}
