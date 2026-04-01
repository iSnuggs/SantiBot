#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;
using Santi.Common;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Roleplay")]
    public partial class RoleplayCommands : SantiModule
    {
        private static readonly SantiRandom _rng = new();
        private static readonly HttpClient _gifHttp = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "SantiBot/1.0" } }
        };

        private static string Pick(string[] pool)
            => pool[_rng.Next(pool.Length)];

        // Helper: build description with optional target
        private static string Desc(IUser user, IUser target, string[] targetLines, string[] soloLines)
            => target is not null
                ? $"{user.Mention} {Pick(targetLines)} {target.Mention}!"
                : $"{user.Mention} {Pick(soloLines)}!";

        // Multi-source GIF mapping: action → (source, category)
        // Source: "nekos" = nekos.best, "waifu" = waifu.pics
        private static readonly Dictionary<string, (string Source, string Category)> _gifSources = new()
        {
            // nekos.best primary sources
            ["hug"]       = ("nekos", "hug"),
            ["pat"]       = ("nekos", "pat"),
            ["kiss"]      = ("nekos", "kiss"),
            ["slap"]      = ("nekos", "slap"),
            ["poke"]      = ("nekos", "poke"),
            ["cuddle"]    = ("nekos", "cuddle"),
            ["wave"]      = ("nekos", "wave"),
            ["highfive"]  = ("nekos", "highfive"),
            ["bite"]      = ("nekos", "bite"),
            ["punch"]     = ("nekos", "punch"),
            ["tickle"]    = ("nekos", "tickle"),
            ["boop"]      = ("nekos", "boop"),
            ["bonk"]      = ("nekos", "bonk"),
            ["lick"]      = ("nekos", "lick"),
            ["stare"]     = ("nekos", "stare"),
            ["cry"]       = ("nekos", "cry"),
            ["dance"]     = ("nekos", "dance"),
            ["yeet"]      = ("nekos", "yeet"),
            ["nuzzle"]    = ("nekos", "nuzzle"),
            ["wink"]      = ("nekos", "wink"),
            ["blush"]     = ("nekos", "blush"),
            ["handshake"] = ("nekos", "handshake"),
            // nekos.best additional
            ["salute"]    = ("nekos", "salute"),
            ["pout"]      = ("nekos", "pout"),
            ["kick"]      = ("nekos", "kick"),
            // nekos.best new actions
            ["angry"]     = ("nekos", "angry"),
            ["baka"]      = ("nekos", "baka"),
            ["blowkiss"]  = ("nekos", "blowkiss"),
            ["carry"]     = ("nekos", "carry"),
            ["clap"]      = ("nekos", "clap"),
            ["confused"]  = ("nekos", "confused"),
            ["facepalm"]  = ("nekos", "facepalm"),
            ["feed"]      = ("nekos", "feed"),
            ["handhold"]  = ("nekos", "handhold"),
            ["happy"]     = ("nekos", "happy"),
            ["laugh"]     = ("nekos", "laugh"),
            ["nom"]       = ("nekos", "nom"),
            ["peck"]      = ("nekos", "peck"),
            ["run"]       = ("nekos", "run"),
            ["shocked"]   = ("nekos", "shocked"),
            ["shoot"]     = ("nekos", "shoot"),
            ["shrug"]     = ("nekos", "shrug"),
            ["sip"]       = ("nekos", "sip"),
            ["sleep"]     = ("nekos", "sleep"),
            ["smile"]     = ("nekos", "smile"),
            ["smug"]      = ("nekos", "smug"),
            ["tableflip"] = ("nekos", "tableflip"),
            ["think"]     = ("nekos", "think"),
            ["thumbsup"]  = ("nekos", "thumbsup"),
            ["yawn"]      = ("nekos", "yawn"),
            // waifu.pics extras
            ["bully"]     = ("waifu", "bully"),
            ["cringe"]    = ("waifu", "cringe"),
            ["glomp"]     = ("waifu", "glomp"),
            ["awoo"]      = ("waifu", "awoo"),
            // Klipy/Giphy search for actions without anime API endpoints
            ["tackle"]    = ("klipy", "anime tackle"),
            ["fistbump"]  = ("klipy", "anime fist bump"),
            ["cheer"]     = ("klipy", "anime cheer"),
            ["bow"]       = ("klipy", "anime bow"),
            ["dab"]       = ("klipy", "anime dab"),
            ["backflip"]  = ("klipy", "anime backflip"),
        };

        /// <summary>
        /// Fetches a random anime GIF from nekos.best or waifu.pics.
        /// Falls back between sources if one is down. Returns null on all failures.
        /// </summary>
        // Klipy API key — sign up free at partner.klipy.com
        private static readonly string _klipyKey = Environment.GetEnvironmentVariable("KLIPY_API_KEY") ?? "";

        private static async Task<string> GetGifAsync(string action)
        {
            if (!_gifSources.TryGetValue(action, out var source))
                return null;

            try
            {
                if (source.Source == "nekos")
                {
                    var json = await _gifHttp.GetStringAsync(
                        $"https://nekos.best/api/v2/{source.Category}");
                    var match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success) return match.Groups[1].Value;

                    // Fallback to waifu.pics if nekos fails
                    json = await _gifHttp.GetStringAsync(
                        $"https://api.waifu.pics/sfw/{source.Category}");
                    match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
                else if (source.Source == "waifu")
                {
                    var json = await _gifHttp.GetStringAsync(
                        $"https://api.waifu.pics/sfw/{source.Category}");
                    var match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
                else if (source.Source == "klipy")
                {
                    var query = Uri.EscapeDataString(source.Category);

                    // Try Klipy (Tenor replacement)
                    if (!string.IsNullOrEmpty(_klipyKey))
                    {
                        try
                        {
                            var klipyJson = await _gifHttp.GetStringAsync(
                                $"https://api.klipy.com/v2/search?q={query}&key={_klipyKey}&limit=20");
                            // Klipy returns escaped URLs like https:\/\/static.klipy.com\/...
                            var klipyMatches = Regex.Matches(klipyJson, "\"mediumgif\":\\{\"url\":\"([^\"]+)\"");
                            if (klipyMatches.Count > 0)
                            {
                                var url = klipyMatches[_rng.Next(klipyMatches.Count)].Groups[1].Value;
                                return url.Replace("\\/", "/");
                            }
                        }
                        catch { /* fall through to Giphy */ }
                    }

                    // Giphy fallback
                    try
                    {
                        var giphyJson = await _gifHttp.GetStringAsync(
                            $"https://api.giphy.com/v1/gifs/random?api_key=0UTRbFtkMxAplrohufYco5IY74U8hOes&tag={query}&rating=g");
                        var giphyMatch = Regex.Match(giphyJson, "\"url\":\\s*\"(https://media[^\"]+\\.gif)\"");
                        if (giphyMatch.Success) return giphyMatch.Groups[1].Value;
                    }
                    catch { /* both failed */ }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch GIF for action {Action} from {Source}", action, source.Source);
            }

            return null;
        }

        // Helper for expanded commands — builds embed with optional target
        private async Task<Discord.EmbedBuilder> BuildRpEmbed(string action, string emoji, string title,
            IUser target, string[] targetLines, string[] soloLines, bool aggressive = false)
        {
            var gifUrl = await GetGifAsync(action);
            var eb = CreateEmbed()
                .WithTitle($"{emoji} {title}")
                .WithDescription(Desc(ctx.User, target, targetLines, soloLines));
            if (aggressive) eb.WithErrorColor(); else eb.WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            return eb;
        }

        // ── 1. Hug ──────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Hug(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("hug", "\U0001F917", "Hug!", target,
                ["wraps their arms around", "gives a big warm hug to", "squeezes tight and hugs", "bear-hugs", "runs up and hugs", "throws their arms wide open and hugs"],
                ["hugs themselves", "wraps their arms around a pillow", "needs a hug", "gives the air a big hug", "hugs an imaginary friend", "sends virtual hugs"]
            )).SendAsync();
        }

        // ── 2. Pat ──────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Pat(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("pat", "\U0001F90D", "Headpat!", target,
                ["gently pats", "gives soft headpats to", "reaches over and pats the head of", "ruffles the hair of", "lovingly headpats", "gives a comforting pat to"],
                ["pats themselves on the head", "gives themselves a headpat", "pats the air gently", "deserves headpats", "pats an invisible friend"]
            )).SendAsync();
        }

        // ── 3. Kiss ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Kiss(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("kiss", "\U0001F48B", "Kiss!", target,
                ["plants a kiss on", "gives a sweet kiss to", "smooshes their lips against", "blows a kiss at", "leans in and kisses", "steals a kiss from"],
                ["blows a kiss into the void", "kisses the air", "sends kisses to everyone", "blows a kiss to the chat", "mwah~"]
            )).SendAsync();
        }

        // ── 4. Slap ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Slap(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("slap", "\U0001F44B", "Slap!", target,
                ["slaps", "smacks", "delivers a mighty slap to", "open-hand slaps", "winds up and slaps", "gives a dramatic slap to"],
                ["slaps the air", "slaps themselves awake", "slaps an invisible mosquito", "swings at nothing", "practices their slap technique"]
            , aggressive: true)).SendAsync();
        }

        // ── 5. Poke ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Poke(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("poke", "\U0001F449", "Poke!", target,
                ["pokes", "gives a sneaky poke to", "jabs their finger at", "repeatedly pokes", "pokes the cheek of", "won't stop poking"],
                ["pokes the air", "pokes themselves", "pokes at nothing", "pokes an invisible wall", "pokes around curiously"]
            )).SendAsync();
        }

        // ── 6. Cuddle ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Cuddle(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("cuddle", "\U0001F97A", "Cuddle!", target,
                ["cuddles up with", "snuggles into the arms of", "curls up next to", "wraps a blanket around themselves and", "gets all cozy with", "pulls close and cuddles"],
                ["cuddles a pillow", "wraps up in a blanket", "curls into a cozy ball", "snuggles into the couch", "needs someone to cuddle"]
            )).SendAsync();
        }

        // ── 7. Wave ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Wave(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("wave", "\U0001F44B", "Wave!", target,
                ["waves at", "gives a friendly wave to", "waves excitedly at", "does a big goofy wave at", "sends a little wave toward", "flails their arms waving at"],
                ["waves to everyone", "waves at the chat", "does a big friendly wave", "waves hello", "waves enthusiastically"]
            )).SendAsync();
        }

        // ── 8. Highfive ─────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Highfive(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("highfive", "\u270B", "High Five!", target,
                ["high-fives", "slaps a crispy high-five with", "jumps up for a high-five with", "goes in for the high-five and nails it with", "throws up their hand and high-fives", "delivers a thunderous high-five to"],
                ["high-fives the air", "leaves someone hanging", "holds up their hand for a high-five", "high-fives themselves", "is looking for a high-five partner"]
            )).SendAsync();
        }

        // ── 9. Bite ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Bite(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("bite", "\U0001F9DB", "Bite!", target,
                ["bites", "takes a nibble on", "chomps down on", "playfully bites", "sinks their teeth into", "goes nom on"],
                ["bites the air", "chomps on nothing", "bites their lip", "gnaws on a snack", "is feeling bitey"]
            , aggressive: true)).SendAsync();
        }

        // ── 10. Punch ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Punch(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("punch", "\U0001F44A", "Punch!", target,
                ["playfully punches", "gives a friendly bop to", "throws a soft punch at", "lightly socks", "shoulder-punches", "delivers a goofy punch to"],
                ["punches the air", "shadow boxes", "throws punches at nothing", "practices their uppercut", "does a one-two combo"]
            , aggressive: true)).SendAsync();
        }

        // ── 11. Tickle ──────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Tickle(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("tickle", "\U0001F923", "Tickle!", target,
                ["tickles", "wiggles their fingers and tickles", "sneaks up and tickles", "launches a tickle attack on", "mercilessly tickles", "finds the ticklish spot on"],
                ["tickles the air", "wiggles their fingers menacingly", "is feeling ticklish", "tickles themselves and regrets it", "is ready to tickle someone"]
            )).SendAsync();
        }

        // ── 12. Boop ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Boop(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("boop", "\U0001F43E", "Boop!", target,
                ["boops the nose of", "gives a gentle nose boop to", "reaches out and boops", "sneaks in a boop on", "boop! right on the nose of", "can't resist booping"],
                ["boops their own nose", "boops the air", "boop~", "is looking for a nose to boop", "boops an invisible snoot"]
            )).SendAsync();
        }

        // ── 13. Bonk ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Bonk(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("bonk", "\U0001F528", "Bonk!", target,
                ["bonks", "bonks the head of", "pulls out a newspaper and bonks", "delivers a righteous bonk to", "goes full bonk mode on", "sends to bonk jail:"],
                ["bonks the air", "bonks themselves", "pulls out the bonk bat", "is ready to bonk someone", "goes to bonk jail voluntarily"]
            , aggressive: true)).SendAsync();
        }

        // ── 14. Lick ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Lick(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("lick", "\U0001F445", "Lick!", target,
                ["licks", "gives a big lick to", "sticks out their tongue and licks", "slurps on", "gives a sloppy lick to", "licks the face of"],
                ["licks the air", "sticks out their tongue", "licks their lips", "is being weird", "does a blep"]
            )).SendAsync();
        }

        // ── 15. Stare ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Stare(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("stare", "\U0001F440", "Stare!", target,
                ["stares intensely at", "locks eyes with", "won't stop staring at", "gives the unblinking stare to", "gazes deeply into the soul of", "squints suspiciously at"],
                ["stares into the void", "stares blankly", "stares at nothing", "spaces out", "gazes into the distance"]
            )).SendAsync();
        }

        // ── 16. Cry ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Cry(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("cry", "\U0001F62D", "Cry!", target,
                ["cries on the shoulder of", "sobs uncontrollably into", "bursts into tears and runs to", "sniffles and clings to", "ugly-cries all over", "needs a tissue and the comfort of"],
                ["cries", "sobs uncontrollably", "bursts into tears", "is having a good cry", "needs a tissue", "ugly-cries alone"]
            )).SendAsync();
        }

        // ── 17. Dance ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Dance(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("dance", "\U0001F57A", "Dance!", target,
                ["dances with", "grabs the hand of and starts dancing with", "busts out some moves with", "does the moonwalk over to", "challenges to a dance-off:", "twirls around the dance floor with"],
                ["busts a move", "dances alone", "hits the dance floor", "starts dancing", "does the moonwalk", "grooves to the music"]
            )).SendAsync();
        }

        // ── 18. Yeet ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Yeet(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("yeet", "\U0001F680", "Yeet!", target,
                ["yeets", "picks up and launches", "grabs and absolutely yeets", "sends flying through the air:", "throws into the stratosphere:", "winds up and yeets the heck out of"],
                ["yeets themselves", "yeets into the void", "yeets an invisible object", "is feeling yeet-y", "YEEEET"]
            , aggressive: true)).SendAsync();
        }

        // ── 19. Tackle ──────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Tackle(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("tackle", "\U0001F3C8", "Tackle!", target,
                ["tackles", "flying-tackles", "runs full speed and tackles", "leaps through the air and tackles", "glomps and tackles", "does a full NFL tackle on"],
                ["tackles the ground", "dives into nothing", "does a flying tackle into a pillow", "practices their tackles", "faceplants"]
            , aggressive: true)).SendAsync();
        }

        // ── 20. Nuzzle ──────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Nuzzle(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("nuzzle", "\U0001F431", "Nuzzle!", target,
                ["nuzzles", "nuzzles up against", "rubs their cheek against", "gives soft nuzzles to", "snuggles their face into", "affectionately nuzzles"],
                ["nuzzles a pillow", "nuzzles into a blanket", "nuzzles the air", "is feeling nuzzly", "rubs their cheek on a soft thing"]
            )).SendAsync();
        }

        // ── 21. Wink ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Wink(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("wink", "\U0001F609", "Wink!", target,
                ["winks at", "gives a sly wink to", "shoots a flirty wink at", "does the finger-guns-and-wink combo at", "winks suggestively at", "throws a cheeky wink toward"],
                ["winks at the chat", "winks", "does a cheeky wink", "gives a sly wink to no one in particular", "winks at everyone"]
            )).SendAsync();
        }

        // ── 22. Fistbump ────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Fistbump(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("fistbump", "\U0001F91C", "Fist Bump!", target,
                ["fist-bumps", "gives an epic fist bump to", "bumps fists with", "does the exploding fist bump with", "brings it in for a fist bump with", "pounds it with"],
                ["fist-bumps the air", "holds out their fist", "is looking for a fist bump", "bumps fists with an invisible friend", "fist bump anyone?"]
            )).SendAsync();
        }

        // ── 23. Salute ──────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Salute(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("salute", "\U0001FAE1", "Salute!", target,
                ["salutes", "stands at attention and salutes", "gives a crisp military salute to", "tips their hat and salutes", "snaps to attention for", "honors with a respectful salute:"],
                ["salutes", "stands at attention", "gives a crisp salute", "salutes the chat", "o7"]
            )).SendAsync();
        }

        // ── 24. Bow ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Bow(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("bow", "\U0001F647", "Bow!", target,
                ["bows respectfully to", "takes a deep bow before", "bows their head to", "does a dramatic theatrical bow for", "kneels and bows to", "gives an elegant bow to"],
                ["takes a bow", "bows gracefully", "does a dramatic bow", "bows to the audience", "takes a theatrical bow"]
            )).SendAsync();
        }

        // ── 25. Cheer ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Cheer(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("cheer", "\U0001F389", "Cheer!", target,
                ["cheers for", "starts a chant for", "pulls out pom-poms and cheers on", "hypes up", "screams words of encouragement at", "rallies the whole chat to cheer for"],
                ["cheers", "lets out a big cheer", "cheers for everyone", "goes WOOO", "is hyped up"]
            )).SendAsync();
        }

        // ── 26. Pout ────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Pout(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("pout", "\U0001F61E", "Pout!", target,
                ["pouts at", "gives the pouty face to", "sticks out their bottom lip at", "huffs and pouts at", "crosses their arms and pouts at", "makes puppy eyes and pouts at"],
                ["pouts", "sticks out their bottom lip", "crosses their arms and pouts", "huffs", "is pouting"]
            )).SendAsync();
        }

        // ── 27. Blush ───────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Blush(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("blush", "\U0001F633", "Blush!", target,
                ["blushes because of", "turns bright red looking at", "can't help blushing around", "goes beet red next to", "hides their flushed face from", "is blushing uncontrollably at"],
                ["blushes", "turns bright red", "goes beet red", "is blushing hard", "hides their flushed face"]
            )).SendAsync();
        }

        // ── 28. Handshake ───────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Handshake(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("handshake", "\U0001F91D", "Handshake!", target,
                ["shakes hands with", "offers a firm handshake to", "extends a hand to", "seals the deal with a handshake with", "gives a professional handshake to", "grips the hand of and shakes it:"],
                ["extends a hand", "offers a handshake to anyone", "holds out their hand", "is looking for a handshake", "practices their firm grip"]
            )).SendAsync();
        }

        // ── 29. Dab ─────────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Dab(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("dab", "\U0001F596", "Dab!", target,
                ["dabs on", "hits the dab at", "drops the filthiest dab on", "dabs aggressively in front of", "double-dabs on", "does a slow-motion dab aimed at"],
                ["hits the dab", "dabs", "drops a filthy dab", "double-dabs", "does a slow-motion dab"]
            )).SendAsync();
        }

        // ── 30. Backflip ────────────────────────────────────────
        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Backflip(IUser target = null)
        {
            await Response().Embed(await BuildRpEmbed("backflip", "\U0001F938", "Backflip!", target,
                ["does a backflip to impress", "lands a perfect backflip in front of", "shows off with a backflip for", "attempts a backflip and eats dirt in front of", "nails a double backflip to flex on", "does a slow-motion backflip while staring at"],
                ["does a backflip", "lands a perfect backflip", "attempts a backflip and eats dirt", "nails a double backflip", "does a slow-motion backflip"]
            )).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  NEW ACTIONS (25 more) — now using MakeRpEmbed helper
        // ═══════════════════════════════════════════════════════════

        private async Task<Discord.EmbedBuilder> MakeRpEmbed(IUser user, IUser target, string action, string emoji, string title, string[] lines, bool aggressive = false)
            => await MakeRpEmbed(user, target, action, emoji, title, lines, null, aggressive);

        private async Task<Discord.EmbedBuilder> MakeRpEmbed(IUser user, IUser target, string action, string emoji, string title, string[] lines, string[] soloLines, bool aggressive = false)
        {
            var gifUrl = await GetGifAsync(action);
            string desc;
            if (target is null && soloLines is not null)
                desc = $"{user.Mention} {soloLines[_rng.Next(soloLines.Length)]}!";
            else if (target is null)
                desc = $"{user.Mention} {lines[_rng.Next(lines.Length)]} the air!";
            else
                desc = $"{user.Mention} {lines[_rng.Next(lines.Length)]} {target.Mention}!";
            var eb = CreateEmbed()
                .WithTitle($"{emoji} {title}")
                .WithDescription(desc);
            if (aggressive) eb.WithErrorColor(); else eb.WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            return eb;
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Angry(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "angry", "😠", "Angry!", ["is furious at", "glares angrily at", "yells at", "fumes at", "is mad at"], ["is furious", "fumes", "is so angry right now", "is seeing red", "rages"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Baka(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "baka", "😤", "Baka!", ["calls", "yells BAKA at", "scolds", "pouts at", "calls an idiot:"], ["yells BAKA", "calls everyone baka", "pouts and says baka", "is frustrated", "BAKA"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Blowkiss(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "blowkiss", "😘", "Blow Kiss!", ["blows a kiss to", "sends a flying kiss to", "winks and blows a kiss at", "throws a kiss toward", "blows a sweet kiss to"], ["blows a kiss", "blows kisses to everyone", "sends a flying kiss to the chat", "mwah~", "blows a kiss into the wind"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Carry(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "carry", "🏋️", "Carry!", ["picks up and carries", "lifts", "scoops up", "princess-carries", "throws over their shoulder:"], ["carries an invisible friend", "flexes their carrying muscles", "picks up a pillow", "is ready to carry someone", "lifts the air"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Clap(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "clap", "👏", "Clap!", ["applauds", "gives a standing ovation to", "claps for", "slow claps at", "cheers on"], ["claps", "gives a round of applause", "does a slow clap", "applauds everyone", "claps enthusiastically"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Confused(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "confused", "😕", "Confused!", ["is confused by", "tilts their head at", "doesn't understand", "gives a puzzled look to", "is bewildered by"], ["is confused", "tilts their head", "is bewildered", "has no idea what's going on", "is puzzled"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Facepalm(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "facepalm", "🤦", "Facepalm!", ["facepalms at", "can't believe", "is done with", "sighs at", "shakes their head at"], ["facepalms", "can't believe it", "is done", "sighs deeply", "shakes their head"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Feed(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "feed", "🍰", "Feed!", ["feeds", "shares food with", "gives a snack to", "stuffs food into", "hand-feeds"], ["eats a snack", "munches on food", "has a treat", "feeds themselves", "nom nom nom"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Handhold(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "handhold", "🤝", "Hold Hands!", ["holds hands with", "grabs the hand of", "intertwines fingers with", "reaches for the hand of", "gently holds"], ["holds out their hand", "reaches out", "wants to hold someone's hand", "extends their hand gently", "is looking for a hand to hold"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Happy(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "happy", "😊", "Happy!", ["is happy because of", "smiles brightly at", "beams with joy at", "is overjoyed around", "feels happy near"], ["is happy", "beams with joy", "is feeling great", "smiles brightly", "radiates happiness"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Laugh(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "laugh", "😂", "Laugh!", ["laughs at", "can't stop laughing at", "cracks up because of", "giggles at", "bursts out laughing with"], ["laughs", "can't stop laughing", "cracks up", "giggles", "bursts out laughing"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Nom(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "nom", "😋", "Nom!", ["noms on", "takes a bite of", "nibbles on", "munches on", "nom nom noms"], ["noms", "nibbles on a snack", "goes nom nom nom", "munches happily", "is nomming"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Peck(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "peck", "💋", "Peck!", ["gives a quick peck to", "pecks", "plants a small kiss on", "lightly kisses", "gives a gentle peck to"], ["gives a peck to the air", "does a little kiss", "pecks at nothing", "mwah", "gives a gentle peck"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpRun(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "run", "🏃", "Run!", ["runs toward", "chases after", "sprints to", "dashes toward", "runs away from"], ["runs away", "sprints off", "dashes away", "takes off running", "makes a break for it"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shocked(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shocked", "😱", "Shocked!", ["is shocked by", "can't believe what", "gasps at", "is stunned by", "jaw drops looking at"], ["is shocked", "gasps", "jaw drops", "is stunned", "can't believe it"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shoot(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shoot", "🔫", "Shoot!", ["finger-guns at", "shoots at", "pulls out the finger guns for", "pew pew pews at", "takes aim at"], ["finger-guns the air", "pew pew", "pulls out the finger guns", "shoots into the sky", "does the finger guns"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shrug(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shrug", "🤷", "Shrug!", ["shrugs at", "doesn't know what to tell", "shrugs toward", "gives a big shrug to", "is indifferent toward"], ["shrugs", "doesn't know", "gives a big shrug", "is indifferent", "shrugs it off"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Sip(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "sip", "☕", "Sip!", ["sips tea while looking at", "takes a slow sip near", "sips judgmentally at", "casually sips around", "drinks tea and side-eyes"], ["sips tea", "takes a long sip", "sips judgmentally", "casually sips", "drinks tea quietly"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Sleep(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "sleep", "😴", "Sleep!", ["falls asleep on", "dozes off next to", "snores beside", "naps on the shoulder of", "falls asleep cuddling"], ["falls asleep", "dozes off", "is sleepy", "takes a nap", "zzz"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Smile(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "smile", "😊", "Smile!", ["smiles warmly at", "gives a big smile to", "grins at", "flashes a smile at", "beams at"], ["smiles", "gives a warm smile", "grins", "is all smiles", "beams happily"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Smug(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "smug", "😏", "Smug!", ["gives a smug look to", "smirks at", "looks smugly at", "grins smugly toward", "has a smug face for"], ["looks smug", "smirks", "has a smug face", "grins smugly", "is feeling smug"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Tableflip(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "tableflip", "╯°□°）╯", "Tableflip!", ["flips a table at", "rage-flips the table toward", "throws the table because of", "╯°□°）╯ ┻━┻ at", "is so done with"], ["flips the table", "╯°□°）╯ ┻━┻", "rage-flips the table", "is so done", "throws the table"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Think(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "think", "🤔", "Think!", ["thinks about", "ponders", "contemplates", "is deep in thought about", "strokes their chin looking at"], ["thinks", "ponders", "contemplates life", "is deep in thought", "strokes their chin"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Thumbsup(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "thumbsup", "👍", "Thumbs Up!", ["gives a thumbs up to", "approves of", "supports", "gives the OK to", "gives two thumbs up to"], ["gives a thumbs up", "approves", "thumbs up everyone", "gives the OK", "👍"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Yawn(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "yawn", "🥱", "Yawn!", ["yawns next to", "is bored by", "yawns because of", "can't stay awake around", "lets out a big yawn near"], ["yawns", "lets out a big yawn", "is sleepy", "can't stop yawning", "yawns loudly"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Bully(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "bully", "😈", "Bully!", ["bullies", "picks on", "teases mercilessly", "won't leave alone:", "messes with"], ["is being a bully", "picks on the air", "teases everyone", "is feeling mischievous", "is up to no good"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Cringe(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "cringe", "😬", "Cringe!", ["cringes at", "can't watch", "winces because of", "is embarrassed by", "dies inside because of"], ["cringes", "dies inside", "winces", "can't even", "is cringing hard"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Glomp(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "glomp", "💨", "Glomp!", ["glomps", "tackle-hugs", "pounces on", "flying-hugs", "launches at"], ["glomps the air", "pounces on nothing", "does a flying hug", "launches forward", "tackle-hugs a pillow"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Awoo(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "awoo", "🐺", "Awoo!", ["awoos at", "howls for", "lets out a big AWOO near", "goes awoo~ at", "howls alongside"], ["lets out a big AWOO", "howls at the moon", "goes awoo~", "AWOOOO", "howls"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpKick(IUser target = null)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "kick", "🦵", "Kick!", ["kicks", "roundhouse kicks", "drop kicks", "gives a swift kick to", "boots"], ["kicks the air", "roundhouse kicks nothing", "practices their kicks", "does a flying kick", "kicks around"], true)).SendAsync(); }
    }
}
