#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("Roleplay 18+")]
    [Group("rp18")]
    public partial class NsfwRoleplayCommands : SantiModule
    {
        private static readonly SantiRandom _rng = new();
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "SantiBot/1.0" } }
        };

        // Klipy API key from env var
        private static readonly string _klipyKey = Environment.GetEnvironmentVariable("KLIPY_API_KEY") ?? "";

        // NSFW-enabled servers opt-in tracking (in-memory, resets on restart)
        // Admins must run .rp18 enable to activate per server
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, bool> _enabledGuilds = new();

        private static string Pick(string[] pool) => pool[_rng.Next(pool.Length)];

        // ── NSFW RP actions with their search terms ──
        // GIF source mapping: (source, category)
        // "nekos" = nekos.best (anime SFW — mood-matched)
        // "nekoslife" = nekos.life (has spank, lewd, kiss NSFW GIFs)
        // "waifunsfw" = waifu.pics /nsfw/ (waifu, neko, blowjob)
        // "purrbot" = purrbot.site /nsfw/ (spank, neko)
        private static readonly Dictionary<string, (string Source, string Category)> _nsfwGifSources = new()
        {
            ["kiss"]      = ("nekoslife", "kiss"),      // nekos.life has NSFW-ish kiss GIFs
            ["cuddle"]    = ("nekoslife", "cuddle"),
            ["lick"]      = ("nekos", "lick"),
            ["bite"]      = ("nekos", "bite"),
            ["spank"]     = ("nekoslife", "spank"),     // nekos.life actual spank GIFs
            ["tease"]     = ("nekos", "smug"),
            ["blush"]     = ("nekos", "blush"),
            ["nuzzle"]    = ("nekos", "nuzzle"),
            ["seduce"]    = ("waifunsfw", "waifu"),     // suggestive anime images
            ["whisper"]   = ("nekos", "peck"),
            ["grind"]     = ("nekos", "dance"),            // intimate dancing
            ["moan"]      = ("nekoslife", "lewd"),      // nekos.life lewd category
            ["strip"]     = ("waifunsfw", "waifu"),     // suggestive anime
            ["dominate"]  = ("purrbot", "spank"),       // purrbot NSFW spank
            ["submit"]    = ("nekoslife", "lewd"),      // suggestive
            ["handcuff"]  = ("nekoslife", "spank"),     // closest NSFW match
            ["blindfold"] = ("waifunsfw", "neko"),      // suggestive neko
            ["tie"]       = ("waifunsfw", "neko"),      // suggestive neko
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
        };

        // ── Gate check — NSFW channel + server opted in ──
        private async Task<bool> CheckNsfwAllowed()
        {
            if (ctx.Channel is not ITextChannel tc || !tc.IsNsfw)
            {
                await Response().Error("This command only works in NSFW channels!").SendAsync();
                return false;
            }

            if (!_enabledGuilds.TryGetValue(ctx.Guild.Id, out var enabled) || !enabled)
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
                    case "nekos":
                        json = await _http.GetStringAsync(
                            $"https://nekos.best/api/v2/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "nekoslife":
                        json = await _http.GetStringAsync(
                            $"https://nekos.life/api/v2/img/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "waifunsfw":
                        json = await _http.GetStringAsync(
                            $"https://api.waifu.pics/nsfw/{source.Category}");
                        match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                        if (match.Success) return match.Groups[1].Value;
                        break;

                    case "purrbot":
                        json = await _http.GetStringAsync(
                            $"https://purrbot.site/api/img/nsfw/{source.Category}/gif");
                        match = Regex.Match(json, "\"link\"\\s*:\\s*\"([^\"]+)\"");
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

            _enabledGuilds[ctx.Guild.Id] = true;
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
            _enabledGuilds.TryRemove(ctx.Guild.Id, out _);
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

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Actions()
        {
            var isNsfw = ctx.Channel is ITextChannel tc && tc.IsNsfw;
            var enabled = _enabledGuilds.TryGetValue(ctx.Guild.Id, out var e) && e;

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
