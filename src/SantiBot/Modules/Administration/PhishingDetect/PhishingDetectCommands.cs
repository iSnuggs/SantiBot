namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("antiphish")]
    [Name("PhishingDetect")]
    public partial class PhishingDetectCommands : SantiModule<PhishingDetectService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AntiPhishEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.antiphish_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AntiPhishDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.antiphish_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AntiPhishAction([Leftover] string action)
        {
            var valid = new[] { "delete", "warn", "mute", "ban" };
            if (!valid.Contains(action.ToLowerInvariant()))
            {
                await Response().Error(strs.antiphish_action_invalid).SendAsync();
                return;
            }

            await _service.SetActionAsync(ctx.Guild.Id, action);
            await Response().Confirm(strs.antiphish_action_set(action)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AntiPhishLog(ITextChannel channel)
        {
            await _service.SetLogChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.antiphish_log_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AntiPhishCheck([Leftover] string url)
        {
            var result = _service.CheckMessageForPhishing(url);

            if (result is not null)
            {
                var eb = CreateEmbed()
                    .WithColor(Color.Red)
                    .WithTitle("Phishing Detected")
                    .WithDescription($"The URL is flagged as suspicious.\n**Reason:** {result}");
                await Response().Embed(eb).SendAsync();
            }
            else
            {
                var eb = CreateEmbed()
                    .WithColor(Color.Green)
                    .WithTitle("URL Appears Safe")
                    .WithDescription("No known phishing patterns matched. This does not guarantee safety.");
                await Response().Embed(eb).SendAsync();
            }
        }
    }
}
