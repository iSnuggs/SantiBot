#nullable disable
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SantiBot.Modules.Social;

public partial class Social
{
    [Name("NSFW Images")]
    [Group("nsfw")]
    public partial class NsfwImageCommands : SantiModule<NsfwRpService>
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "SantiBot/1.0" } }
        };

        private async Task<bool> CheckNsfw()
        {
            if (ctx.Channel is not ITextChannel tc || !tc.IsNsfw)
            {
                await Response().Error("This command only works in NSFW channels!").SendAsync();
                return false;
            }
            if (!await _service.IsEnabledAsync(ctx.Guild.Id))
            {
                await Response().Error("NSFW features not enabled! Admin: `.rp18 enable` in an NSFW channel.").SendAsync();
                return false;
            }
            return true;
        }

        private async Task PostImage(string title, string emoji, string apiUrl, string urlField, Discord.Color color)
        {
            if (!await CheckNsfw()) return;
            try
            {
                var json = await _http.GetStringAsync(apiUrl);
                var match = Regex.Match(json, $"\"{urlField}\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success)
                {
                    var url = match.Groups[1].Value.Replace("\\/", "/");
                    var eb = CreateEmbed()
                        .WithTitle($"{emoji} {title}")
                        .WithImageUrl(url)
                        .WithColor(color)
                        .WithFooter("NSFW • Only visible in age-gated channels");
                    await Response().Embed(eb).SendAsync();
                    return;
                }
            }
            catch { }
            await Response().Error("Couldn't fetch an image right now.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Waifu()
            => await PostImage("Waifu", "💕", "https://api.waifu.pics/nsfw/waifu", "url", new Discord.Color(0xFF69B4));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Neko()
            => await PostImage("Neko", "😺", "https://api.waifu.pics/nsfw/neko", "url", new Discord.Color(0xFF1493));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Lewd()
            => await PostImage("Lewd", "🔞", "https://nekos.life/api/v2/img/lewd", "url", new Discord.Color(0xFF4500));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Hentai()
            => await PostImage("Hentai", "🔞", "https://nekos.life/api/v2/img/lewd", "url", new Discord.Color(0x8B0000));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Porn()
            => await PostImage("Porn GIF", "🔥", "https://nekobot.xyz/api/image?type=pgif", "message", new Discord.Color(0xFF4500));

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Actions()
        {
            var isNsfw = ctx.Channel is ITextChannel tc && tc.IsNsfw;
            var enabled = await _service.IsEnabledAsync(ctx.Guild.Id);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("**NSFW Image Commands** (`.nsfw <command>`)\n");
            sb.AppendLine("  💕 `.nsfw waifu` — Random NSFW waifu");
            sb.AppendLine("  😺 `.nsfw neko` — Random NSFW neko");
            sb.AppendLine("  🔞 `.nsfw lewd` — Random lewd image");
            sb.AppendLine("  🔞 `.nsfw hentai` — Random hentai image");
            sb.AppendLine("  🔥 `.nsfw porn` — Random NSFW GIF");
            sb.AppendLine($"\n**Status:** {(enabled ? "🟢 Enabled" : "🔴 Disabled")}");
            sb.AppendLine($"**NSFW Channel:** {(isNsfw ? "✅ Yes" : "❌ No")}");
            if (!enabled)
                sb.AppendLine("\n*Admin: `.rp18 enable` in an NSFW channel to activate*");

            var eb = CreateEmbed()
                .WithTitle("🔞 NSFW Images")
                .WithDescription(sb.ToString())
                .WithColor(new Discord.Color(0xFF1493));
            await Response().Embed(eb).SendAsync();
        }
    }
}
