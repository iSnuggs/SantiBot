#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Roleplay 18+")]
    [Group("rp18")]
    public partial class NsfwRoleplayCommands : SantiModule<NsfwRpService>
    {
        private static readonly SantiRandom _rng = new();
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "SantiBot/1.0" } }
        };

        // Klipy API key from env var
        private static readonly string _klipyKey = Environment.GetEnvironmentVariable("KLIPY_API_KEY") ?? "";

        // In-memory cache of enabled guilds (loaded from DB on first check)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _enabledGuilds = new();
        private static bool _cacheLoaded;

        private static string Pick(string[] pool) => pool[_rng.Next(pool.Length)];

        // ── NSFW RP actions with their search terms ──
        // GIF source mapping: (source, category)
        // "nekos" = nekos.best (anime SFW — mood-matched)
        // "nekoslife" = nekos.life (has spank, lewd, kiss NSFW GIFs)
        // "waifunsfw" = waifu.pics /nsfw/ (waifu, neko, blowjob)
        // "purrbot" = purrbot.site /nsfw/ (spank, neko)
        // GIF sources: nekos = nekos.best, nekoslife = nekos.life, waifunsfw = waifu.pics/nsfw
        // purrbot = purrbot.site, otaku = otakugifs.xyz, waifuim = waifu.im, nekobot = nekobot.xyz
        private static readonly Dictionary<string, (string Source, string Category)> _nsfwGifSources = new()
        {
            ["kiss"]      = ("otaku", "kiss"),           // otakugifs — great kiss GIFs
            ["cuddle"]    = ("otaku", "cuddle"),          // otakugifs cuddle
            ["lick"]      = ("otaku", "lick"),            // otakugifs lick
            ["bite"]      = ("otaku", "bite"),            // otakugifs bite
            ["spank"]     = ("purrbot", "spank"),         // purrbot NSFW spank GIFs
            ["tease"]     = ("nekos", "smug"),
            ["blush"]     = ("otaku", "blush"),           // otakugifs blush
            ["nuzzle"]    = ("nekos", "nuzzle"),          // nekos.best nuzzle
            ["seduce"]    = ("waifuim", "ero"),           // waifu.im ero — suggestive
            ["whisper"]   = ("nekos", "peck"),
            ["grind"]     = ("nekobot", "pgif"),            // nekobot NSFW GIF — intimate
            ["moan"]      = ("nekobot", "pgif"),            // nekobot NSFW GIF
            ["strip"]     = ("nekobot", "pgif"),            // nekobot NSFW GIF — undressing
            ["dominate"]  = ("nekobot", "pgif"),            // nekobot NSFW GIF — dominant
            ["submit"]    = ("nekobot", "pgif"),            // nekobot NSFW GIF — submissive
            ["handcuff"]  = ("nekobot", "pgif"),            // nekobot NSFW GIF — bondage
            ["blindfold"] = ("nekobot", "pgif"),            // nekobot NSFW GIF — bondage
            ["tie"]       = ("nekobot", "pgif"),            // nekobot NSFW GIF — bondage
            ["caress"]    = ("otaku", "peck"),            // otakugifs gentle touch
            ["pin"]       = ("nekobot", "pgif"),            // nekobot NSFW GIF — pinning
            ["collar"]    = ("nekobot", "pgif"),            // nekobot NSFW GIF — collar
            ["praise"]    = ("otaku", "pat"),             // otakugifs wholesome
            ["punish"]    = ("nekobot", "pgif"),            // nekobot NSFW GIF — punishment
            ["claim"]     = ("otaku", "bite"),            // otakugifs possessive
            ["undress"]   = ("waifunsfw", "waifu"),          // waifu.pics NSFW waifu — suggestive
            ["worship"]   = ("otaku", "kiss"),            // otakugifs devotional
            ["makeout"]   = ("otaku", "kiss"),            // otakugifs intense kiss
            ["leash"]     = ("nekobot", "pgif"),            // nekobot NSFW GIF — leash
        };

        // Keep this for the Actions list display
        private static readonly Dictionary<string, string> _nsfwActions = _nsfwGifSources.Keys
            .ToDictionary(k => k, k => k);

        // Flavor text per action
        private static readonly Dictionary<string, string[]> _lines = new()
        {
            ["kiss"] = ["passionately kisses", "pulls close and kisses", "locks lips with", "steals a deep kiss from", "can't resist kissing"],
            ["cuddle"] = ["cuddles up intimately with", "pulls close and holds", "wraps around", "snuggles deeply with", "holds tight against"],
            ["lick"] = ["teasingly licks", "runs their tongue across", "gives a slow lick to", "playfully licks", "can't help but lick"],
            ["bite"] = ["bites down on", "sinks teeth gently into", "nibbles on", "gives a naughty bite to", "playfully bites"],
            ["spank"] = ["gives a firm spank to", "spanks", "smacks the rear of", "delivers a spank to", "gives a playful swat to"],
            ["tease"] = ["teasingly pokes at", "flirts shamelessly with", "gives a teasing look to", "can't stop teasing", "provokes"],
            ["blush"] = ["blushes deeply looking at", "turns bright red around", "can't hide their blush near", "gets flustered by", "feels their face heat up near"],
            ["nuzzle"] = ["nuzzles intimately against", "presses close to", "rubs against", "buries into", "snuggles into the neck of"],
            ["seduce"] = ["gives bedroom eyes to", "seductively winks at", "tries to charm", "puts on their best moves for", "flirts dangerously with"],
            ["whisper"] = ["whispers something naughty to", "leans in and whispers to", "murmurs softly to", "breathes sweet words to", "whispers secrets to"],
            ["grind"] = ["grinds against", "presses up against", "moves rhythmically with", "gets close and grinds on", "dances intimately with"],
            ["moan"] = ["moans softly near", "lets out a soft moan for", "can't hold back a moan around", "moans breathlessly at", "whimpers softly for"],
            ["strip"] = ["performs a strip tease for", "slowly undresses for", "puts on a show for", "teases with a slow reveal for", "strips seductively for"],
            ["dominate"] = ["dominates", "takes control of", "pins down", "asserts dominance over", "commands"],
            ["submit"] = ["submits to", "kneels before", "surrenders to", "gives in to", "yields to"],
            ["handcuff"] = ["handcuffs", "restrains", "cuffs the wrists of", "clicks handcuffs on", "binds"],
            ["blindfold"] = ["blindfolds", "covers the eyes of", "ties a blindfold on", "takes away the sight of", "blindfolds and teases"],
            ["tie"] = ["ties up", "binds the hands of", "restrains with rope", "ties the wrists of", "binds and teases"],
            ["caress"] = ["gently caresses {0}'s cheek, fingertips barely touching skin...", "traces slow, tender lines along {0}'s arm, savoring every inch.", "caresses {0} softly, leaving a trail of warmth wherever they touch.", "'s hands drift across {0}'s back in long, deliberate strokes.", "cups {0}'s face and caresses them like they're something precious."],
            ["pin"] = ["pins {0} against the wall, eyes dark with intent. 'Going somewhere?'", "grabs {0}'s wrists and pins them above their head with a slow smirk.", "presses {0} down, leaning in close. 'Stay still.'", "pins {0} in place — firm, deliberate, no room to escape.", "has {0} pinned and isn't planning on letting go anytime soon."],
            ["collar"] = ["fastens a collar around {0}'s neck and steps back to admire it. 'Perfect.'", "reaches for {0}'s collar and tugs them closer without a word.", "slips a collar around {0}. A quiet claim. A quiet promise.", "adjusts {0}'s collar slowly, eyes never leaving theirs.", "collars {0} with a soft click. They belong to each other now."],
            ["praise"] = ["tilts {0}'s chin up. 'You did so well. I'm proud of you.'", "murmurs 'such a good boy/girl' against {0}'s ear.", "strokes {0}'s hair softly. 'That's it. Just like that. Good.'", "breathes 'you're perfect, you know that?' clearly pleased with {0}.", "pulls {0} close and whispers praise that makes their whole body warm."],
            ["punish"] = ["says quietly 'you knew the rules,' turning to face {0} with a look.", "decides {0} needs a reminder of who's in charge tonight.", "asks {0} 'did I say you could do that?' tilting their head slowly.", "makes {0} very aware that their behavior has consequences.", "tried {0}'s patience. Now they're about to find out what that costs."],
            ["claim"] = ["pulls {0} close by the waist. 'Mine.' Simple. Final.", "marks {0} — a silent declaration of ownership.", "says 'everyone needs to know,' leaving their claim on {0} for all to see.", "holds {0} possessively, daring anyone to say otherwise.", "looks at {0} with that familiar intensity. 'You're mine. Don't forget it.'"],
            ["undress"] = ["reaches for the hem of {0}'s shirt, eyes asking permission — then proceeding.", "undresses {0} slow and deliberate, like they have all the time in the world.", "one button at a time, undresses {0}, savoring every reveal.", "murmurs 'relax' as they undress {0} carefully.", "slides {0}'s jacket off their shoulders and lets it drop to the floor."],
            ["worship"] = ["drops to their knees before {0}. 'Let me show you how much I adore you.'", "intends to give every inch of {0} the reverence they deserve.", "presses slow kisses along {0}'s collarbone, worshipping every inch.", "murmurs 'you deserve to be worshipped' to {0}, lips moving across their skin.", "worships {0} with hands and lips and every ounce of devotion they have."],
            ["makeout"] = ["grabs {0} and pulls them into a deep, breathless kiss that says everything.", "starts slow — then pulls {0} closer and things escalate quickly.", "and {0} lose track of time, lips locked, hands wandering.", "cups {0}'s face and kisses them deeply, thoroughly, completely.", "answers every breath from {0} by kissing them harder."],
            ["leash"] = ["clips a leash to {0}'s collar and gives a gentle tug. 'Come.'", "fastens the leash to {0}. 'You don't go anywhere without me.'", "wraps the leash around their hand and leads {0} without a word.", "watches {0} follow obediently — the leash a reminder of who holds the lead.", "trails the leash through their fingers, watching {0} with quiet satisfaction."],
        };

        // ── Gate check — NSFW channel + server opted in ──
        private async Task<bool> CheckNsfwAllowed()
        {
            if (ctx.Channel is not ITextChannel tc || !tc.IsNsfw)
            {
                await Response().Error("This command only works in NSFW channels!").SendAsync();
                return false;
            }

            if (!await _service.IsEnabledAsync(ctx.Guild.Id))
            {
                await Response().Error(
                    "NSFW roleplay is not enabled on this server!\n" +
                    "An admin must run `.rp18 enable` in an NSFW channel first.").SendAsync();
                return false;
            }

            return true;
        }

        // ── Fetch GIF from multiple NSFW anime APIs ──
        private static async Task<string> GetNsfwGifAsync(string action)
        {
            if (!_nsfwGifSources.TryGetValue(action, out var source))
                return null;

            try
            {
                string json;
                System.Text.RegularExpressions.Match match;

                switch (source.Source)
                {
                    case "nekos": // nekos.best — SFW anime GIFs
                        json = await _http.GetStringAsync(
                            $"https://nekos.best/api/v2/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "nekoslife": // nekos.life — has some NSFW categories
                        json = await _http.GetStringAsync(
                            $"https://nekos.life/api/v2/img/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "waifunsfw": // waifu.pics NSFW
                        json = await _http.GetStringAsync(
                            $"https://api.waifu.pics/nsfw/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "purrbot": // purrbot.site — SFW + NSFW GIFs
                        json = await _http.GetStringAsync(
                            $"https://purrbot.site/api/img/nsfw/{source.Category}/gif");
                        match = Regex.Match(json, "\"link\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "otaku": // otakugifs.xyz — free, no key, great categories
                        json = await _http.GetStringAsync(
                            $"https://api.otakugifs.xyz/gif?reaction={source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "waifuim": // waifu.im — tags like ero, ecchi, waifu
                        json = await _http.GetStringAsync(
                            $"https://api.waifu.im/images?included_tags={source.Category}&is_nsfw=true");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"(https://cdn[^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "nekobot": // nekobot.xyz — hentai, boobs, ass, pgif
                        json = await _http.GetStringAsync(
                            $"https://nekobot.xyz/api/image?type={source.Category}");
                        match = Regex.Match(json, "\"message\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;
                }
            }
            catch { /* API failed, no GIF */ }

            return null;
        }

        // ── Admin: Enable/Disable ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Enable()
        {
            if (ctx.Channel is not ITextChannel tc || !tc.IsNsfw)
            {
                await Response().Error("Run this command in an NSFW channel to enable.").SendAsync();
                return;
            }

            await _service.SetEnabledAsync(ctx.Guild.Id, true);
            await Response().Confirm(
                "🔞 **NSFW Roleplay enabled** for this server.\n" +
                "Commands only work in NSFW-marked channels.\n" +
                "Use `.rp18 disable` to turn off.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Disable()
        {
            await _service.SetEnabledAsync(ctx.Guild.Id, false);
            await Response().Confirm("NSFW Roleplay disabled for this server.").SendAsync();
        }

        // ── Action Commands ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Kiss(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("kiss");
            var eb = CreateEmbed()
                .WithTitle("💋 Kiss")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["kiss"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cuddle(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("cuddle");
            var eb = CreateEmbed()
                .WithTitle("💕 Cuddle")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["cuddle"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Lick(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("lick");
            var eb = CreateEmbed()
                .WithTitle("👅 Lick")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["lick"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bite(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("bite");
            var eb = CreateEmbed()
                .WithTitle("😈 Bite")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["bite"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF4500));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Spank(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("spank");
            var eb = CreateEmbed()
                .WithTitle("🍑 Spank")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["spank"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF4500));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Tease(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("tease");
            var eb = CreateEmbed()
                .WithTitle("😏 Tease")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["tease"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Blush(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("blush");
            var eb = CreateEmbed()
                .WithTitle("😳 Blush")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["blush"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Nuzzle(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("nuzzle");
            var eb = CreateEmbed()
                .WithTitle("🥰 Nuzzle")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["nuzzle"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Seduce(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("seduce");
            var eb = CreateEmbed()
                .WithTitle("😘 Seduce")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["seduce"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Whisper(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("whisper");
            var eb = CreateEmbed()
                .WithTitle("🤫 Whisper")
                .WithDescription($"{ctx.User.Mention} {Pick(_lines["whisper"])} {target.Mention}!")
                .WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Grind(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("grind");
            var eb = CreateEmbed().WithTitle("💃 Grind").WithDescription($"{ctx.User.Mention} {Pick(_lines["grind"])} {target.Mention}!").WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Moan(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("moan");
            var eb = CreateEmbed().WithTitle("😩 Moan").WithDescription($"{ctx.User.Mention} {Pick(_lines["moan"])} {target.Mention}!").WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Strip(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("strip");
            var eb = CreateEmbed().WithTitle("🔥 Strip").WithDescription($"{ctx.User.Mention} {Pick(_lines["strip"])} {target.Mention}!").WithColor(new Discord.Color(0xFF1493));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Dominate(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("dominate");
            var eb = CreateEmbed().WithTitle("⛓️ Dominate").WithDescription($"{ctx.User.Mention} {Pick(_lines["dominate"])} {target.Mention}!").WithColor(new Discord.Color(0x8B0000));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Submit(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("submit");
            var eb = CreateEmbed().WithTitle("🧎 Submit").WithDescription($"{ctx.User.Mention} {Pick(_lines["submit"])} {target.Mention}!").WithColor(new Discord.Color(0xFF69B4));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Handcuff(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("handcuff");
            var eb = CreateEmbed().WithTitle("🔗 Handcuff").WithDescription($"{ctx.User.Mention} {Pick(_lines["handcuff"])} {target.Mention}!").WithColor(new Discord.Color(0x8B0000));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Blindfold(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("blindfold");
            var eb = CreateEmbed().WithTitle("🙈 Blindfold").WithDescription($"{ctx.User.Mention} {Pick(_lines["blindfold"])} {target.Mention}!").WithColor(new Discord.Color(0x8B0000));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Tie(IUser target)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync("tie");
            var eb = CreateEmbed().WithTitle("🪢 Tie Up").WithDescription($"{ctx.User.Mention} {Pick(_lines["tie"])} {target.Mention}!").WithColor(new Discord.Color(0x8B0000));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        // Helper for new commands — Santi's flavor text uses {0} for target name
        private async Task NsfwRpAsync(string action, string emoji, string title, IUser target, uint color)
        {
            if (!await CheckNsfwAllowed()) return;
            var gif = await GetNsfwGifAsync(action);
            var line = string.Format(Pick(_lines[action]), target.Mention);
            var eb = CreateEmbed().WithTitle($"{emoji} {title}").WithDescription($"{ctx.User.Mention} {line}").WithColor(new Discord.Color(color));
            if (gif is not null) eb.WithImageUrl(gif);
            await Response().Embed(eb).SendAsync();
        }

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Caress(IUser target) => await NsfwRpAsync("caress", "✋", "Caress", target, 0xFF69B4);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Pin(IUser target) => await NsfwRpAsync("pin", "📌", "Pin", target, 0x8B0000);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Collar(IUser target) => await NsfwRpAsync("collar", "🔗", "Collar", target, 0x8B0000);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Praise(IUser target) => await NsfwRpAsync("praise", "🌟", "Praise", target, 0xFF69B4);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Punish(IUser target) => await NsfwRpAsync("punish", "⚡", "Punish", target, 0x8B0000);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Claim(IUser target) => await NsfwRpAsync("claim", "🔥", "Claim", target, 0xFF1493);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Undress(IUser target) => await NsfwRpAsync("undress", "👗", "Undress", target, 0xFF1493);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Worship(IUser target) => await NsfwRpAsync("worship", "🙏", "Worship", target, 0xFF69B4);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Makeout(IUser target) => await NsfwRpAsync("makeout", "💋", "Makeout", target, 0xFF1493);

        [Cmd] [RequireContext(ContextType.Guild)]
        public async Task Leash(IUser target) => await NsfwRpAsync("leash", "🪢", "Leash", target, 0x8B0000);

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Actions()
        {
            var isNsfw = ctx.Channel is ITextChannel tc && tc.IsNsfw;
            var enabled = await _service.IsEnabledAsync(ctx.Guild.Id);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("**NSFW Roleplay Actions** (`.rp18 <action> @user`)\n");

            foreach (var action in _nsfwActions.Keys)
                sb.AppendLine($"  💋 `.rp18 {action} @user`");

            sb.AppendLine($"\n**Status:** {(enabled ? "🟢 Enabled" : "🔴 Disabled")}");
            sb.AppendLine($"**NSFW Channel:** {(isNsfw ? "✅ Yes" : "❌ No — must be in NSFW channel")}");

            if (!enabled)
                sb.AppendLine("\n*Admin: `.rp18 enable` in an NSFW channel to activate*");

            var eb = CreateEmbed()
                .WithTitle("🔞 NSFW Roleplay")
                .WithDescription(sb.ToString())
                .WithColor(new Discord.Color(0xFF1493));
            await Response().Embed(eb).SendAsync();
        }
    }
}
