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
        private static readonly Dictionary<string, string> _nsfwActions = new()
        {
            ["kiss"]     = "anime kiss nsfw",
            ["cuddle"]   = "anime cuddle nsfw",
            ["lick"]     = "anime lick nsfw",
            ["bite"]     = "anime bite nsfw",
            ["spank"]    = "anime spank",
            ["tease"]    = "anime tease",
            ["blush"]    = "anime blush nsfw",
            ["nuzzle"]   = "anime nuzzle nsfw",
            ["seduce"]   = "anime seduce",
            ["whisper"]  = "anime whisper nsfw",
        };

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

        // ── Fetch GIF from Klipy ──
        private static async Task<string> GetNsfwGifAsync(string action)
        {
            if (!_nsfwActions.TryGetValue(action, out var query))
                return null;

            try
            {
                var encoded = Uri.EscapeDataString(query);

                if (!string.IsNullOrEmpty(_klipyKey))
                {
                    try
                    {
                        var json = await _http.GetStringAsync(
                            $"https://api.klipy.com/v2/search?q={encoded}&key={_klipyKey}&limit=20");
                        var matches = Regex.Matches(json, "\"mediumgif\":\\{\"url\":\"([^\"]+)\"");
                        if (matches.Count > 0)
                            return matches[_rng.Next(matches.Count)].Groups[1].Value.Replace("\\/", "/");
                    }
                    catch { /* fall through */ }
                }

                // Giphy fallback
                try
                {
                    var json = await _http.GetStringAsync(
                        $"https://api.giphy.com/v1/gifs/random?api_key=0UTRbFtkMxAplrohufYco5IY74U8hOes&tag={encoded}&rating=r");
                    var match = Regex.Match(json, "\"url\":\\s*\"(https://media[^\"]+\\.gif)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
                catch { /* both failed */ }
            }
            catch { /* outer catch */ }

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
