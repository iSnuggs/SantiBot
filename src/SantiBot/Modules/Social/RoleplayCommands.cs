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
            // nekos.best extras
            ["lurk"]      = ("nekos", "lurk"),
            // otakugifs extras
            ["sing"]      = ("otaku", "sing"),
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
                else if (source.Source == "otaku")
                {
                    var json = await _gifHttp.GetStringAsync(
                        $"https://api.otakugifs.xyz/gif?reaction={source.Category}");
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

        // ═══════════════════════════════════════════════════════════
        //  SANTI'S NEW RP COMMANDS (23 more) — uses {0} format for target
        // ═══════════════════════════════════════════════════════════

        // Helper: Santi's flavor text uses {0} placeholder for target mention
        private async Task SantiRpAsync(string action, string emoji, string title, IUser target,
            string[] targetLines, string[] soloLines, bool aggressive = false)
        {
            var gifUrl = await GetGifAsync(action);
            string desc;
            if (target is not null)
                desc = string.Format(Pick(targetLines), target.Mention);
            else
                desc = Pick(soloLines);
            var eb = CreateEmbed()
                .WithTitle($"{emoji} {title}")
                .WithDescription($"{ctx.User.Mention} {desc}");
            if (aggressive) eb.WithErrorColor(); else eb.WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Squeeze(IUser target = null) => await SantiRpAsync("hug", "🫂", "Squeeze!", target,
            ["squeezes {0} tight, refusing to let go.", "grabs {0} and squeezes like their life depends on it.", "gives {0} the most aggressive hug-squeeze combo known to mankind.", "squeezes {0} until they make a little noise."],
            ["squeezes a pillow tight", "squeezes the air desperately", "needs someone to squeeze", "gives themselves a squeeze"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Comfort(IUser target = null) => await SantiRpAsync("hug", "💛", "Comfort!", target,
            ["sits next to {0} and gently puts an arm around them. 'Hey. I've got you.'", "pulls {0} into a soft hug without saying a word.", "says quietly 'it's going to be okay,' squeezing {0}'s shoulder.", "makes {0} tea, wraps them in a blanket, and sits with them.", "doesn't say anything — just shows up for {0}. Sometimes that's enough."],
            ["wraps up in a blanket", "takes a deep breath", "is being gentle with themselves today", "sits quietly and breathes"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Protect(IUser target = null) => await SantiRpAsync("stare", "🛡️", "Protect!", target,
            ["steps in front of {0}, arms spread. 'You'll have to go through me first.'", "has decided nobody is touching {0} today. Personally.", "stands between {0} and the world. Ride or die.", "announces '{0} is under my protection now' to absolutely no one.", "wraps a protective arm around {0} and glares at everything nearby."],
            ["activates protect mode", "stands guard", "is protecting everyone", "shields up"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Snuggle(IUser target = null) => await SantiRpAsync("cuddle", "🥰", "Snuggle!", target,
            ["burrows into {0}'s side like a small determined animal.", "and {0} achieve maximum snuggle. No one is moving. This is home now.", "drags {0} into a blanket nest and refuses to release them.", "announces '{0} is warm and I have decided to snuggle them.'", "snuggles {0} aggressively. This is not a request."],
            ["snuggles into a blanket", "curls up in a cozy ball", "achieves maximum snuggle solo", "snuggles a pillow aggressively"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Headbutt(IUser target = null) => await SantiRpAsync("bonk", "🤕", "Headbutt!", target,
            ["gently bonks their forehead against {0}'s. Affection achieved.", "headbutts {0} lovingly like a small goat. BONK.", "presses their forehead to {0}'s. It means 'I like you' in chaotic.", "gives {0} a gentle forehead bonk. Consider yourself loved.", "headbutts {0} softly. It's basically a hug but with their head."],
            ["headbutts the air", "bonks their head gently on the wall", "does a little headbutt into nothing", "headbutts a pillow"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Nibble(IUser target = null) => await SantiRpAsync("bite", "😬", "Nibble!", target,
            ["gives {0} a little nibble. Playful. Harmless. Slightly feral.", "nibbles {0}. Please remain calm.", "nibbles {0}'s ear gently. Nom.", "couldn't help it. {0} looked too nibbleable.", "nibbles {0} affectionately like a hamster who loves you."],
            ["nibbles on a snack", "nibbles the air", "is feeling nibblish", "nom nom nom"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Yoink(IUser target = null) => await SantiRpAsync("yeet", "🫳", "Yoink!", target,
            ["yoinks {0}'s snack/hat/dignity and bolts.", "swipes {0}'s thing with zero hesitation and maximum confidence. YOINK.", "yoinks something from {0} and sprints. Classic.", "blinks at {0}. Something is gone. Already three rooms away."],
            ["yoinks something from the void", "yoinks the air", "grabs at nothing", "YOINK"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Kidnap(IUser target = null) => await SantiRpAsync("carry", "🚐", "Kidnap!", target,
            ["throws a blanket over {0} and drags them away. 'You're coming with me.'", "has kidnapped {0}. Destination: unknown. Snacks: maybe.", "says '{0}, you're needed,' already pulling them out the door.", "grabs {0} by the wrist dramatically. 'No time to explain. Run.'", "has claimed {0}. They seem fine with it honestly."],
            ["kidnaps themselves", "runs off dramatically alone", "throws a blanket over nothing", "is planning a kidnapping"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpThrow(IUser target = null) => await SantiRpAsync("yeet", "🏈", "Throw!", target,
            ["picks up {0} and yeets them across the room. With love.", "LAUNCHES {0} like a bowling ball. Impressive form.", "shouts '{0} INCOMING!' already mid-throw.", "spins once for momentum then releases {0} into the void."],
            ["throws something into the void", "launches an invisible object", "YEET", "throws with all their might at nothing"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpChallenge(IUser target = null) => await SantiRpAsync("punch", "⚔️", "Challenge!", target,
            ["points at {0} dramatically. 'You. Me. Right now. Let's go.'", "declares 'I challenge you' to {0}, slamming a gauntlet they definitely own.", "cracks their knuckles and stares {0} down. It's on.", "squares up to {0}. This beef has been officially started."],
            ["challenges the void", "issues a challenge to no one", "cracks their knuckles menacingly", "is looking for a fight"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Flex(IUser target = null) => await SantiRpAsync("dab", "💪", "Flex!", target,
            ["flexes on {0} with zero remorse.", "turns to {0} and begins flexing. The message is clear.", "asks {0} 'you see this?' gesturing vaguely at themselves. 'All of this.'", "strikes a pose directly at {0}. Consider yourself flexed upon."],
            ["flexes", "strikes a pose", "flexes on everyone", "begins flexing. Confidence: maximum"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpIgnore(IUser target = null) => await SantiRpAsync("shrug", "🙄", "Ignore!", target,
            ["walks right past {0} without acknowledgment. Devastating.", "stares directly over {0}'s head into the middle distance.", "pretends {0} does not exist. Oscar-worthy performance.", "activates the silent treatment. {0} is being ignored."],
            ["ignores everything", "stares into the distance", "pretends no one exists", "is ignoring the world"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Stalk(IUser target = null) => await SantiRpAsync("lurk", "🕵️", "Stalk!", target,
            ["follows {0} at a suspicious distance, pretending to look at their phone.", "is 10 steps behind {0}. Casually. Definitely not on purpose.", "ducks behind a plant as {0} turns around. Suspicious.", "has been tailing {0} for 20 minutes and counting."],
            ["lurks suspiciously", "hides behind a plant", "is being very suspicious", "watches from the shadows"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Forehead(IUser target = null) => await SantiRpAsync("kiss", "😚", "Forehead Kiss!", target,
            ["presses a soft kiss to {0}'s forehead. Gentle. Pure.", "leans in and kisses {0}'s forehead tenderly. 'There.'", "gives {0} a forehead kiss. They have been blessed.", "cups {0}'s face and presses their lips to their forehead softly.", "gives {0} a forehead kiss that says everything. No words needed."],
            ["kisses the air gently", "sends forehead kisses to everyone", "presses a kiss to an invisible forehead", "mwah~"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Nap(IUser target = null) => await SantiRpAsync("sleep", "💤", "Nap!", target,
            ["falls asleep on {0}'s shoulder. Do not move. Do not breathe.", "has claimed {0}'s lap as their pillow. This is permanent now.", "curls up on {0} and is asleep within seconds. Without warning.", "naps on {0} with complete confidence and zero apology.", "mumbles '{0}'s lap is the best pillow' before passing out entirely."],
            ["takes a nap", "curls up and falls asleep", "is napping", "zzz... naptime"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Share(IUser target = null) => await SantiRpAsync("feed", "🤝", "Share!", target,
            ["silently slides their snack over to {0}. No words needed.", "splits their last piece with {0}. That's real friendship.", "says 'here,' handing over half their food to {0} without hesitation.", "shares their blanket with {0}. Maximum warmth achieved.", "is sharing with {0} anyway. That's just how they are."],
            ["shares with the void", "offers snacks to everyone", "is feeling generous", "shares their blanket with no one"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Serenade(IUser target = null) => await SantiRpAsync("sing", "🎤", "Serenade!", target,
            ["clears their throat and begins serenading {0}. Quality TBD.", "grabs an invisible microphone and serenades {0} with full commitment.", "sings to {0} like no one is watching. Everyone is watching.", "performs directly in {0}'s face. For you~"],
            ["serenades the void", "sings to no one", "belts out a tune", "grabs an invisible mic and goes for it"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Propose(IUser target = null) => await SantiRpAsync("happy", "💍", "Propose!", target,
            ["drops to one knee in front of {0}. The room goes silent.", "asks {0} 'will you?' holding out something small and sparkly.", "proposes to {0} with full drama and zero warning.", "looks at {0} with complete sincerity. 'Will you be mine?'"],
            ["proposes to the air", "drops to one knee dramatically", "holds out an invisible ring", "is proposing to no one"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task RpDivorce(IUser target = null) => await SantiRpAsync("tableflip", "📄", "Divorce!", target,
            ["slides divorce papers across the table to {0}. 'It's over.'", "says 'I want my blanket back,' starting the proceedings against {0}.", "has filed for divorce from {0}. Reason: vibes.", "announces 'I'm leaving' to {0} dramatically. Walks 3 feet away."],
            ["files for divorce from no one", "slides papers across the table dramatically", "is done. Just done.", "announces a divorce to the void"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Groom(IUser target = null) => await SantiRpAsync("pat", "🐱", "Groom!", target,
            ["begins grooming {0}'s hair like a cat who has decided to adopt them.", "licks their hand and smooths down {0}'s hair. You're welcome.", "grooms {0} with the dedication of a cat who takes hygiene seriously.", "whether {0} wanted it or not, the grooming has been handled."],
            ["grooms themselves", "licks their paw and fixes their hair", "is self-grooming", "smooths down their own hair"]);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Hiss(IUser target = null) => await SantiRpAsync("angry", "🐍", "Hiss!", target,
            ["hisses at {0}. Do not approach.", "has triggered their hiss response at {0}. Back up.", "puffs up and hisses at {0} like a very small, very dramatic cat.", "directs a HISSSSS specifically at {0}."],
            ["hisses at the world", "hisses at nothing", "HISSSSS", "puffs up and hisses at everything"], true);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Headpat(IUser target = null) => await SantiRpAsync("pat", "🤚", "Headpat!", target,
            ["reaches over and gives {0} a very deliberate headpat.", "has headpatted {0}. They're good now. Pat. Pat. Pat.", "places their hand on {0}'s head with complete authority and pats.", "gives {0} the most sincere headpat they have ever received."],
            ["pats their own head", "headpats the air", "is looking for a head to pat", "gives themselves a headpat"]);
    }
}

