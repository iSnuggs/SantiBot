#nullable disable
using Santi.Common;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Roleplay")]
    [Group("rp")]
    public partial class RoleplayCommands : SantiModule
    {
        private static readonly SantiRandom _rng = new();

        private static string Pick(string[] pool)
            => pool[_rng.Next(pool.Length)];

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
            var eb = CreateEmbed()
                .WithTitle("\U0001F917 Hug!")
                .WithDescription($"{ctx.User.Mention} {Pick(_hugLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F90D Headpat!")
                .WithDescription($"{ctx.User.Mention} {Pick(_patLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F48B Kiss!")
                .WithDescription($"{ctx.User.Mention} {Pick(_kissLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F44B Slap!")
                .WithDescription($"{ctx.User.Mention} {Pick(_slapLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F449 Poke!")
                .WithDescription($"{ctx.User.Mention} {Pick(_pokeLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F97A Cuddle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cuddleLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F44B Wave!")
                .WithDescription($"{ctx.User.Mention} {Pick(_waveLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\u270B High Five!")
                .WithDescription($"{ctx.User.Mention} {Pick(_highfiveLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F9DB Bite!")
                .WithDescription($"{ctx.User.Mention} {Pick(_biteLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F44A Punch!")
                .WithDescription($"{ctx.User.Mention} {Pick(_punchLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F923 Tickle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_tickleLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F43E Boop!")
                .WithDescription($"{ctx.User.Mention} {Pick(_boopLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F528 Bonk!")
                .WithDescription($"{ctx.User.Mention} {Pick(_bonkLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F445 Lick!")
                .WithDescription($"{ctx.User.Mention} {Pick(_lickLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F440 Stare!")
                .WithDescription($"{ctx.User.Mention} {Pick(_stareLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F62D Cry!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cryLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F57A Dance!")
                .WithDescription($"{ctx.User.Mention} {Pick(_danceLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F680 Yeet!")
                .WithDescription($"{ctx.User.Mention} {Pick(_yeetLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F3C8 Tackle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_tackleLines)} {target.Mention}!")
                .WithErrorColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F431 Nuzzle!")
                .WithDescription($"{ctx.User.Mention} {Pick(_nuzzleLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F609 Wink!")
                .WithDescription($"{ctx.User.Mention} {Pick(_winkLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F91C Fist Bump!")
                .WithDescription($"{ctx.User.Mention} {Pick(_fistbumpLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001FAE1 Salute!")
                .WithDescription($"{ctx.User.Mention} {Pick(_saluteLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F647 Bow!")
                .WithDescription($"{ctx.User.Mention} {Pick(_bowLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F389 Cheer!")
                .WithDescription($"{ctx.User.Mention} {Pick(_cheerLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F61E Pout!")
                .WithDescription($"{ctx.User.Mention} {Pick(_poutLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F633 Blush!")
                .WithDescription($"{ctx.User.Mention} {Pick(_blushLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F91D Handshake!")
                .WithDescription($"{ctx.User.Mention} {Pick(_handshakeLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F596 Dab!")
                .WithDescription($"{ctx.User.Mention} {Pick(_dabLines)} {target.Mention}!")
                .WithOkColor();
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
            var eb = CreateEmbed()
                .WithTitle("\U0001F938 Backflip!")
                .WithDescription($"{ctx.User.Mention} {Pick(_backflipLines)} {target.Mention}!")
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
