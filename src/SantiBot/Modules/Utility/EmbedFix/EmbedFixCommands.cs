#nullable disable
using SantiBot.Modules.Utility.EmbedFix;

namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Embed Fix")]
    [Group("embedfix")]
    public partial class EmbedFixCommands : SantiModule<EmbedFixService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Enable()
        {
            await _service.SetEnabledAsync(ctx.Guild.Id, true);
            await Response().Confirm(
                "📎 **Embed Fix enabled!**\n\n" +
                "SantiBot will automatically fix broken embeds from:\n" +
                "🐦 Twitter/X → fxtwitter.com\n" +
                "📸 Instagram → kkinstagram.com\n" +
                "🎵 TikTok → vxtiktok.com\n" +
                "🤖 Reddit → rxddit.com\n" +
                "🦋 Bluesky → fxbsky.app\n\n" +
                "Use `.embedfix toggle <platform>` to enable/disable specific platforms.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Disable()
        {
            await _service.SetEnabledAsync(ctx.Guild.Id, false);
            await Response().Confirm("Embed fix disabled.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Toggle([Leftover] string platform)
        {
            platform = platform?.Trim().ToLower();
            if (!EmbedFixService.Platforms.ContainsKey(platform))
            {
                var valid = string.Join(", ", EmbedFixService.Platforms.Keys.Select(k => $"`{k}`"));
                await Response().Error($"Unknown platform! Available: {valid}").SendAsync();
                return;
            }

            await _service.TogglePlatformAsync(ctx.Guild.Id, platform);
            var settings = _service.GetSettings(ctx.Guild.Id);
            var state = settings?.EnabledPlatforms?.Contains(platform) == true ? "enabled" : "disabled";
            var info = EmbedFixService.Platforms[platform];
            await Response().Confirm($"{info.Emoji} **{info.Name}** embed fix is now **{state}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task DeleteOriginal()
        {
            var settings = _service.GetSettings(ctx.Guild.Id);
            var newState = settings is null || !settings.DeleteOriginal;
            await _service.SetDeleteOriginalAsync(ctx.Guild.Id, newState);
            await Response().Confirm(
                newState
                    ? "Original messages with links will be **deleted** and reposted with fixed embeds."
                    : "Original messages will be **kept** (embeds suppressed). Fixed link posted separately."
            ).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var settings = _service.GetSettings(ctx.Guild.Id);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**Embed Fix:** {(settings?.Enabled == true ? "🟢 Enabled" : "🔴 Disabled")}");
            sb.AppendLine($"**Delete Original:** {(settings?.DeleteOriginal == true ? "Yes" : "No (suppress embeds)")}");
            sb.AppendLine();
            sb.AppendLine("**Platforms:**");

            foreach (var (key, (name, domain, emoji)) in EmbedFixService.Platforms)
            {
                var on = settings?.EnabledPlatforms?.Contains(key) == true;
                sb.AppendLine($"  {emoji} {name}: {(on ? "✅" : "❌")} → `{domain}`");
            }

            sb.AppendLine();
            sb.AppendLine("**How it works:**");
            sb.AppendLine("When someone posts a Twitter/X/Instagram/TikTok/Reddit link,");
            sb.AppendLine("SantiBot reposts it with a fixed embed that actually shows the content.");

            var eb = CreateEmbed()
                .WithTitle("📎 Embed Fix Status")
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Fix([Leftover] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await Response().Error("Provide a URL to fix! `.embedfix fix https://x.com/...`").SendAsync();
                return;
            }

            var fixedUrl = url;
            foreach (var (key, (_, domain, _)) in EmbedFixService.Platforms)
            {
                fixedUrl = key switch
                {
                    "twitter" => System.Text.RegularExpressions.Regex.Replace(fixedUrl, @"(twitter\.com|x\.com)", domain, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    "instagram" => System.Text.RegularExpressions.Regex.Replace(fixedUrl, @"instagram\.com", domain, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    "tiktok" => System.Text.RegularExpressions.Regex.Replace(fixedUrl, @"tiktok\.com", domain, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    "reddit" => System.Text.RegularExpressions.Regex.Replace(fixedUrl, @"reddit\.com", domain, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    "bluesky" => System.Text.RegularExpressions.Regex.Replace(fixedUrl, @"bsky\.app", domain, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    _ => fixedUrl,
                };
            }

            if (fixedUrl == url)
            {
                await Response().Error("No supported platform detected in that URL.").SendAsync();
                return;
            }

            await Response().Confirm($"📎 **Fixed:** {fixedUrl}").SendAsync();
        }
    }
}
