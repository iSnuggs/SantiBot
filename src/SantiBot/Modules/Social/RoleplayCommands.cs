#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;
using Santi.Common;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Roleplay")]
    [Group("rp")]
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

        // ── 1. Hug ──────────────────────────────────────────────
        private static readonly string[] _hugLines =
        {
            "wraps their arms around",
            "gives a big warm hug to",
            "squeezes tight and hugs",
            "bear-hugs",
            "runs up and hugs",
            "throws their arms wide open and hugs"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hug(IUser target)
        {
            var gifUrl = await GetGifAsync("hug");
            var eb = CreateEmbed()
                .WithTitle("\U0001F917 Hug!")
                .WithDescription($"{ctx.User.Mention} {Pick(_hugLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 2. Pat ──────────────────────────────────────────────
        private static readonly string[] _patLines =
        {
            "gently pats",
            "gives soft headpats to",
            "reaches over and pats the head of",
            "ruffles the hair of",
            "lovingly headpats",
            "gives a comforting pat to"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Pat(IUser target)
        {
            var gifUrl = await GetGifAsync("pat");
            var eb = CreateEmbed()
                .WithTitle("\U0001F90D Headpat!")
                .WithDescription($"{ctx.User.Mention} {Pick(_patLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 3. Kiss ─────────────────────────────────────────────
        private static readonly string[] _kissLines =
        {
            "plants a kiss on",
            "gives a sweet kiss to",
            "smooshes their lips against",
            "blows a kiss at",
            "leans in and kisses",
            "steals a kiss from"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Kiss(IUser target)
        {
            var gifUrl = await GetGifAsync("kiss");
            var eb = CreateEmbed()
                .WithTitle("\U0001F48B Kiss!")
                .WithDescription($"{ctx.User.Mention} {Pick(_kissLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 4. Slap ─────────────────────────────────────────────
        private static readonly string[] _slapLines =
        {
            "slaps",
            "smacks",
            "delivers a mighty slap to",
            "open-hand slaps",
            "winds up and slaps",
            "gives a dramatic slap to"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Slap(IUser target)
        {
            var gifUrl = await GetGifAsync("slap");
            var eb = CreateEmbed()
                .WithTitle("\U0001F44B Slap!")
                .WithDescription($"{ctx.User.Mention} {Pick(_slapLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 5. Poke ─────────────────────────────────────────────
        private static readonly string[] _pokeLines =
        {
            "pokes",
            "gives a sneaky poke to",
            "jabs their finger at",
            "repeatedly pokes",
            "pokes the cheek of",
            "won't stop poking"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Poke(IUser target)
        {
            var gifUrl = await GetGifAsync("poke");
            var eb = CreateEmbed()
                .WithTitle("\U0001F449 Poke!")
                .WithDescription($"{ctx.User.Mention} {Pick(_pokeLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 6. Cuddle ───────────────────────────────────────────
        private static readonly string[] _cuddleLines =
        {
            "cuddles up with",
            "snuggles into the arms of",
            "curls up next to",
            "wraps a blanket around themselves and",
            "gets all cozy with",
            "pulls close and cuddles"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cuddle(IUser target)
        {
            var gifUrl = await GetGifAsync("cuddle");
            var eb = CreateEmbed()
                .WithTitle("\U0001F97A Cuddle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cuddleLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 7. Wave ─────────────────────────────────────────────
        private static readonly string[] _waveLines =
        {
            "waves at",
            "gives a friendly wave to",
            "waves excitedly at",
            "does a big goofy wave at",
            "sends a little wave toward",
            "flails their arms waving at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Wave(IUser target)
        {
            var gifUrl = await GetGifAsync("wave");
            var eb = CreateEmbed()
                .WithTitle("\U0001F44B Wave!")
                .WithDescription($"{ctx.User.Mention} {Pick(_waveLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 8. Highfive ─────────────────────────────────────────
        private static readonly string[] _highfiveLines =
        {
            "high-fives",
            "slaps a crispy high-five with",
            "jumps up for a high-five with",
            "goes in for the high-five and nails it with",
            "throws up their hand and high-fives",
            "delivers a thunderous high-five to"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Highfive(IUser target)
        {
            var gifUrl = await GetGifAsync("highfive");
            var eb = CreateEmbed()
                .WithTitle("\u270B High Five!")
                .WithDescription($"{ctx.User.Mention} {Pick(_highfiveLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 9. Bite ─────────────────────────────────────────────
        private static readonly string[] _biteLines =
        {
            "bites",
            "takes a nibble on",
            "chomps down on",
            "playfully bites",
            "sinks their teeth into",
            "goes nom on"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bite(IUser target)
        {
            var gifUrl = await GetGifAsync("bite");
            var eb = CreateEmbed()
                .WithTitle("\U0001F9DB Bite!")
                .WithDescription($"{ctx.User.Mention} {Pick(_biteLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 10. Punch ───────────────────────────────────────────
        private static readonly string[] _punchLines =
        {
            "playfully punches",
            "gives a friendly bop to",
            "throws a soft punch at",
            "lightly socks",
            "shoulder-punches",
            "delivers a goofy punch to"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Punch(IUser target)
        {
            var gifUrl = await GetGifAsync("punch");
            var eb = CreateEmbed()
                .WithTitle("\U0001F44A Punch!")
                .WithDescription($"{ctx.User.Mention} {Pick(_punchLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 11. Tickle ──────────────────────────────────────────
        private static readonly string[] _tickleLines =
        {
            "tickles",
            "wiggles their fingers and tickles",
            "sneaks up and tickles",
            "launches a tickle attack on",
            "mercilessly tickles",
            "finds the ticklish spot on"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Tickle(IUser target)
        {
            var gifUrl = await GetGifAsync("tickle");
            var eb = CreateEmbed()
                .WithTitle("\U0001F923 Tickle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_tickleLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 12. Boop ────────────────────────────────────────────
        private static readonly string[] _boopLines =
        {
            "boops the nose of",
            "gives a gentle nose boop to",
            "reaches out and boops",
            "sneaks in a boop on",
            "boop! right on the nose of",
            "can't resist booping"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Boop(IUser target)
        {
            var gifUrl = await GetGifAsync("boop");
            var eb = CreateEmbed()
                .WithTitle("\U0001F43E Boop!")
                .WithDescription($"{ctx.User.Mention} {Pick(_boopLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 13. Bonk ────────────────────────────────────────────
        private static readonly string[] _bonkLines =
        {
            "bonks",
            "bonks the head of",
            "pulls out a newspaper and bonks",
            "delivers a righteous bonk to",
            "goes full bonk mode on",
            "sends to bonk jail:"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bonk(IUser target)
        {
            var gifUrl = await GetGifAsync("bonk");
            var eb = CreateEmbed()
                .WithTitle("\U0001F528 Bonk!")
                .WithDescription($"{ctx.User.Mention} {Pick(_bonkLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 14. Lick ────────────────────────────────────────────
        private static readonly string[] _lickLines =
        {
            "licks",
            "gives a big lick to",
            "sticks out their tongue and licks",
            "slurps on",
            "gives a sloppy lick to",
            "licks the face of"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Lick(IUser target)
        {
            var gifUrl = await GetGifAsync("lick");
            var eb = CreateEmbed()
                .WithTitle("\U0001F445 Lick!")
                .WithDescription($"{ctx.User.Mention} {Pick(_lickLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 15. Stare ───────────────────────────────────────────
        private static readonly string[] _stareLines =
        {
            "stares intensely at",
            "locks eyes with",
            "won't stop staring at",
            "gives the unblinking stare to",
            "gazes deeply into the soul of",
            "squints suspiciously at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Stare(IUser target)
        {
            var gifUrl = await GetGifAsync("stare");
            var eb = CreateEmbed()
                .WithTitle("\U0001F440 Stare!")
                .WithDescription($"{ctx.User.Mention} {Pick(_stareLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 16. Cry ─────────────────────────────────────────────
        private static readonly string[] _cryLines =
        {
            "cries on the shoulder of",
            "sobs uncontrollably into",
            "bursts into tears and runs to",
            "sniffles and clings to",
            "ugly-cries all over",
            "needs a tissue and the comfort of"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cry(IUser target)
        {
            var gifUrl = await GetGifAsync("cry");
            var eb = CreateEmbed()
                .WithTitle("\U0001F62D Cry!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cryLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 17. Dance ───────────────────────────────────────────
        private static readonly string[] _danceLines =
        {
            "dances with",
            "grabs the hand of and starts dancing with",
            "busts out some moves with",
            "does the moonwalk over to",
            "challenges to a dance-off:",
            "twirls around the dance floor with"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Dance(IUser target)
        {
            var gifUrl = await GetGifAsync("dance");
            var eb = CreateEmbed()
                .WithTitle("\U0001F57A Dance!")
                .WithDescription($"{ctx.User.Mention} {Pick(_danceLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 18. Yeet ────────────────────────────────────────────
        private static readonly string[] _yeetLines =
        {
            "yeets",
            "picks up and launches",
            "grabs and absolutely yeets",
            "sends flying through the air:",
            "throws into the stratosphere:",
            "winds up and yeets the heck out of"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Yeet(IUser target)
        {
            var gifUrl = await GetGifAsync("yeet");
            var eb = CreateEmbed()
                .WithTitle("\U0001F680 Yeet!")
                .WithDescription($"{ctx.User.Mention} {Pick(_yeetLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 19. Tackle ──────────────────────────────────────────
        private static readonly string[] _tackleLines =
        {
            "tackles",
            "flying-tackles",
            "runs full speed and tackles",
            "leaps through the air and tackles",
            "glomps and tackles",
            "does a full NFL tackle on"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Tackle(IUser target)
        {
            var gifUrl = await GetGifAsync("tackle");
            var eb = CreateEmbed()
                .WithTitle("\U0001F3C8 Tackle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_tackleLines)} {target.Mention}!")
                .WithErrorColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 20. Nuzzle ──────────────────────────────────────────
        private static readonly string[] _nuzzleLines =
        {
            "nuzzles",
            "nuzzles up against",
            "rubs their cheek against",
            "gives soft nuzzles to",
            "snuggles their face into",
            "affectionately nuzzles"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Nuzzle(IUser target)
        {
            var gifUrl = await GetGifAsync("nuzzle");
            var eb = CreateEmbed()
                .WithTitle("\U0001F431 Nuzzle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_nuzzleLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 21. Wink ────────────────────────────────────────────
        private static readonly string[] _winkLines =
        {
            "winks at",
            "gives a sly wink to",
            "shoots a flirty wink at",
            "does the finger-guns-and-wink combo at",
            "winks suggestively at",
            "throws a cheeky wink toward"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Wink(IUser target)
        {
            var gifUrl = await GetGifAsync("wink");
            var eb = CreateEmbed()
                .WithTitle("\U0001F609 Wink!")
                .WithDescription($"{ctx.User.Mention} {Pick(_winkLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 22. Fistbump ────────────────────────────────────────
        private static readonly string[] _fistbumpLines =
        {
            "fist-bumps",
            "gives an epic fist bump to",
            "bumps fists with",
            "does the exploding fist bump with",
            "brings it in for a fist bump with",
            "pounds it with"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fistbump(IUser target)
        {
            var gifUrl = await GetGifAsync("fistbump");
            var eb = CreateEmbed()
                .WithTitle("\U0001F91C Fist Bump!")
                .WithDescription($"{ctx.User.Mention} {Pick(_fistbumpLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 23. Salute ──────────────────────────────────────────
        private static readonly string[] _saluteLines =
        {
            "salutes",
            "stands at attention and salutes",
            "gives a crisp military salute to",
            "tips their hat and salutes",
            "snaps to attention for",
            "honors with a respectful salute:"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Salute(IUser target)
        {
            var gifUrl = await GetGifAsync("salute");
            var eb = CreateEmbed()
                .WithTitle("\U0001FAE1 Salute!")
                .WithDescription($"{ctx.User.Mention} {Pick(_saluteLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 24. Bow ─────────────────────────────────────────────
        private static readonly string[] _bowLines =
        {
            "bows respectfully to",
            "takes a deep bow before",
            "bows their head to",
            "does a dramatic theatrical bow for",
            "kneels and bows to",
            "gives an elegant bow to"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bow(IUser target)
        {
            var gifUrl = await GetGifAsync("bow");
            var eb = CreateEmbed()
                .WithTitle("\U0001F647 Bow!")
                .WithDescription($"{ctx.User.Mention} {Pick(_bowLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 25. Cheer ───────────────────────────────────────────
        private static readonly string[] _cheerLines =
        {
            "cheers for",
            "starts a chant for",
            "pulls out pom-poms and cheers on",
            "hypes up",
            "screams words of encouragement at",
            "rallies the whole chat to cheer for"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cheer(IUser target)
        {
            var gifUrl = await GetGifAsync("cheer");
            var eb = CreateEmbed()
                .WithTitle("\U0001F389 Cheer!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cheerLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 26. Pout ────────────────────────────────────────────
        private static readonly string[] _poutLines =
        {
            "pouts at",
            "gives the pouty face to",
            "sticks out their bottom lip at",
            "huffs and pouts at",
            "crosses their arms and pouts at",
            "makes puppy eyes and pouts at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Pout(IUser target)
        {
            var gifUrl = await GetGifAsync("pout");
            var eb = CreateEmbed()
                .WithTitle("\U0001F61E Pout!")
                .WithDescription($"{ctx.User.Mention} {Pick(_poutLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 27. Blush ───────────────────────────────────────────
        private static readonly string[] _blushLines =
        {
            "blushes because of",
            "turns bright red looking at",
            "can't help blushing around",
            "goes beet red next to",
            "hides their flushed face from",
            "is blushing uncontrollably at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Blush(IUser target)
        {
            var gifUrl = await GetGifAsync("blush");
            var eb = CreateEmbed()
                .WithTitle("\U0001F633 Blush!")
                .WithDescription($"{ctx.User.Mention} {Pick(_blushLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 28. Handshake ───────────────────────────────────────
        private static readonly string[] _handshakeLines =
        {
            "shakes hands with",
            "offers a firm handshake to",
            "extends a hand to",
            "seals the deal with a handshake with",
            "gives a professional handshake to",
            "grips the hand of and shakes it:"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Handshake(IUser target)
        {
            var gifUrl = await GetGifAsync("handshake");
            var eb = CreateEmbed()
                .WithTitle("\U0001F91D Handshake!")
                .WithDescription($"{ctx.User.Mention} {Pick(_handshakeLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 29. Dab ─────────────────────────────────────────────
        private static readonly string[] _dabLines =
        {
            "dabs on",
            "hits the dab at",
            "drops the filthiest dab on",
            "dabs aggressively in front of",
            "double-dabs on",
            "does a slow-motion dab aimed at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Dab(IUser target)
        {
            var gifUrl = await GetGifAsync("dab");
            var eb = CreateEmbed()
                .WithTitle("\U0001F596 Dab!")
                .WithDescription($"{ctx.User.Mention} {Pick(_dabLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ── 30. Backflip ────────────────────────────────────────
        private static readonly string[] _backflipLines =
        {
            "does a backflip to impress",
            "lands a perfect backflip in front of",
            "shows off with a backflip for",
            "attempts a backflip and eats dirt in front of",
            "nails a double backflip to flex on",
            "does a slow-motion backflip while staring at"
        };

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Backflip(IUser target)
        {
            var gifUrl = await GetGifAsync("backflip");
            var eb = CreateEmbed()
                .WithTitle("\U0001F938 Backflip!")
                .WithDescription($"{ctx.User.Mention} {Pick(_backflipLines)} {target.Mention}!")
                .WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            await Response().Embed(eb).SendAsync();
        }

        // ═══════════════════════════════════════════════════════════
        //  NEW ACTIONS (25 more)
        // ═══════════════════════════════════════════════════════════

        private async Task<Discord.EmbedBuilder> MakeRpEmbed(IUser user, IUser target, string action, string emoji, string title, string[] lines, bool aggressive = false)
        {
            var gifUrl = await GetGifAsync(action);
            var eb = CreateEmbed()
                .WithTitle($"{emoji} {title}")
                .WithDescription($"{user.Mention} {lines[_rng.Next(lines.Length)]} {target.Mention}!");
            if (aggressive) eb.WithErrorColor(); else eb.WithOkColor();
            if (gifUrl is not null) eb.WithImageUrl(gifUrl);
            return eb;
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Angry(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "angry", "😠", "Angry!", ["is furious at", "glares angrily at", "yells at", "fumes at", "is mad at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Baka(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "baka", "😤", "Baka!", ["calls", "yells BAKA at", "scolds", "pouts at", "calls an idiot:"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Blowkiss(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "blowkiss", "😘", "Blow Kiss!", ["blows a kiss to", "sends a flying kiss to", "winks and blows a kiss at", "throws a kiss toward", "blows a sweet kiss to"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Carry(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "carry", "🏋️", "Carry!", ["picks up and carries", "lifts", "scoops up", "princess-carries", "throws over their shoulder:"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Clap(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "clap", "👏", "Clap!", ["applauds", "gives a standing ovation to", "claps for", "slow claps at", "cheers on"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Confused(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "confused", "😕", "Confused!", ["is confused by", "tilts their head at", "doesn't understand", "gives a puzzled look to", "is bewildered by"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Facepalm(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "facepalm", "🤦", "Facepalm!", ["facepalms at", "can't believe", "is done with", "sighs at", "shakes their head at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Feed(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "feed", "🍰", "Feed!", ["feeds", "shares food with", "gives a snack to", "stuffs food into", "hand-feeds"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Handhold(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "handhold", "🤝", "Hold Hands!", ["holds hands with", "grabs the hand of", "intertwines fingers with", "reaches for the hand of", "gently holds"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Happy(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "happy", "😊", "Happy!", ["is happy because of", "smiles brightly at", "beams with joy at", "is overjoyed around", "feels happy near"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Laugh(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "laugh", "😂", "Laugh!", ["laughs at", "can't stop laughing at", "cracks up because of", "giggles at", "bursts out laughing with"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Nom(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "nom", "😋", "Nom!", ["noms on", "takes a bite of", "nibbles on", "munches on", "nom nom noms"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Peck(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "peck", "💋", "Peck!", ["gives a quick peck to", "pecks", "plants a small kiss on", "lightly kisses", "gives a gentle peck to"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Run(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "run", "🏃", "Run!", ["runs toward", "chases after", "sprints to", "dashes toward", "runs away from"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shocked(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shocked", "😱", "Shocked!", ["is shocked by", "can't believe what", "gasps at", "is stunned by", "jaw drops looking at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shoot(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shoot", "🔫", "Shoot!", ["finger-guns at", "shoots at", "pulls out the finger guns for", "pew pew pews at", "takes aim at"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Shrug(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "shrug", "🤷", "Shrug!", ["shrugs at", "doesn't know what to tell", "shrugs toward", "gives a big shrug to", "is indifferent toward"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Sip(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "sip", "☕", "Sip!", ["sips tea while looking at", "takes a slow sip near", "sips judgmentally at", "casually sips around", "drinks tea and side-eyes"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Sleep(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "sleep", "😴", "Sleep!", ["falls asleep on", "dozes off next to", "snores beside", "naps on the shoulder of", "falls asleep cuddling"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Smile(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "smile", "😊", "Smile!", ["smiles warmly at", "gives a big smile to", "grins at", "flashes a smile at", "beams at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Smug(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "smug", "😏", "Smug!", ["gives a smug look to", "smirks at", "looks smugly at", "grins smugly toward", "has a smug face for"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Tableflip(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "tableflip", "╯°□°）╯", "Tableflip!", ["flips a table at", "rage-flips the table toward", "throws the table because of", "╯°□°）╯ ┻━┻ at", "is so done with"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Think(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "think", "🤔", "Think!", ["thinks about", "ponders", "contemplates", "is deep in thought about", "strokes their chin looking at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Thumbsup(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "thumbsup", "👍", "Thumbs Up!", ["gives a thumbs up to", "approves of", "supports", "gives the OK to", "gives two thumbs up to"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Yawn(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "yawn", "🥱", "Yawn!", ["yawns next to", "is bored by", "yawns because of", "can't stay awake around", "lets out a big yawn near"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Bully(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "bully", "😈", "Bully!", ["bullies", "picks on", "teases mercilessly", "won't leave alone:", "messes with"], true)).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Cringe(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "cringe", "😬", "Cringe!", ["cringes at", "can't watch", "winces because of", "is embarrassed by", "dies inside because of"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Glomp(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "glomp", "💨", "Glomp!", ["glomps", "tackle-hugs", "pounces on", "flying-hugs", "launches at"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Awoo(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "awoo", "🐺", "Awoo!", ["awoos at", "howls for", "lets out a big AWOO near", "goes awoo~ at", "howls alongside"])).SendAsync(); }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Kick(IUser target)
        { await Response().Embed(await MakeRpEmbed(ctx.User, target, "kick", "🦵", "Kick!", ["kicks", "roundhouse kicks", "drop kicks", "gives a swift kick to", "boots"], true)).SendAsync(); }
    }
}
