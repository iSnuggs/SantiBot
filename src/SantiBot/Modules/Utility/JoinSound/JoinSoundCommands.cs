namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("joinsound")]
    [Name("JoinSound")]
    public partial class JoinSoundCommands : SantiModule<JoinSoundService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task JoinSoundSet([Leftover] string url)
        {
            if (string.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://") && !url.StartsWith("https://")))
            {
                await Response().Error(strs.joinsound_invalid_url).SendAsync();
                return;
            }

            await _service.SetUserSoundAsync(ctx.Guild.Id, ctx.User.Id, url.Trim());
            await Response().Confirm(strs.joinsound_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task JoinSoundRemove()
        {
            if (await _service.RemoveUserSoundAsync(ctx.Guild.Id, ctx.User.Id))
                await Response().Confirm(strs.joinsound_removed).SendAsync();
            else
                await Response().Error(strs.joinsound_not_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task JoinSoundDefault([Leftover] string url)
        {
            if (string.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://") && !url.StartsWith("https://")))
            {
                await Response().Error(strs.joinsound_invalid_url).SendAsync();
                return;
            }

            await _service.SetDefaultSoundAsync(ctx.Guild.Id, url.Trim());
            await Response().Confirm(strs.joinsound_default_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task JoinSoundEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.joinsound_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task JoinSoundDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.joinsound_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task JoinSoundMaxDuration(int seconds)
        {
            if (seconds < 1 || seconds > 15)
            {
                await Response().Error(strs.joinsound_duration_invalid).SendAsync();
                return;
            }

            await _service.SetMaxDurationAsync(ctx.Guild.Id, seconds);
            await Response().Confirm(strs.joinsound_duration_set(seconds)).SendAsync();
        }
    }
}
