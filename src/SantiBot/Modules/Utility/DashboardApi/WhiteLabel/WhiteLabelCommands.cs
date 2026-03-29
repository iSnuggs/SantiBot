namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("WhiteLabel")]
    [Group("whitelabel")]
    public partial class WhiteLabelCommands : SantiModule<DashboardApi.WhiteLabel.WhiteLabelService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WhiteLabelName([Leftover] string name)
        {
            await _service.SetNameAsync(ctx.Guild.Id, name);
            await Response().Confirm($"Bot name set to **{name}** in this server.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WhiteLabelAvatar([Leftover] string url)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await Response().Error("Invalid URL.").SendAsync();
                return;
            }

            await _service.SetAvatarAsync(ctx.Guild.Id, url);
            await Response().Confirm("Bot avatar URL saved for this server's branding.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WhiteLabelColor([Leftover] string hexColor)
        {
            hexColor = hexColor.TrimStart('#');
            if (hexColor.Length is not (3 or 6) || !hexColor.All(c => "0123456789abcdefABCDEF".Contains(c)))
            {
                await Response().Error("Invalid hex color. Use format: `#FF5733` or `FF5733`").SendAsync();
                return;
            }

            await _service.SetColorAsync(ctx.Guild.Id, hexColor);
            await Response().Confirm($"Primary color set to `#{hexColor}`").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WhiteLabelReset()
        {
            await _service.ResetAsync(ctx.Guild.Id);
            await Response().Confirm("White-label branding has been reset.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task WhiteLabelView()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Confirm("No custom branding configured for this server.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithOkColor()
                .WithTitle("White-Label Branding")
                .AddField("Bot Name", config.BotName ?? "*Default*", true)
                .AddField("Primary Color", config.PrimaryColor is not null ? $"#{config.PrimaryColor}" : "*Default*", true)
                .AddField("Avatar URL", config.AvatarUrl ?? "*Default*", false);

            await Response().Embed(embed).SendAsync();
        }
    }
}
