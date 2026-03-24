#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class WelcomeImageCommands : SantiModule<WelcomeImageService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImg()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            var newState = config is null || !config.Enabled;
            await _service.EnableAsync(ctx.Guild.Id, newState);
            await Response().Confirm(strs.welcomeimg_toggled(newState ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImgChannel(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.welcomeimg_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImgBg([Leftover] string url)
        {
            await _service.SetBackgroundAsync(ctx.Guild.Id, url);
            await Response().Confirm(strs.welcomeimg_bg_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImgColor(string hexColor)
        {
            await _service.SetColorAsync(ctx.Guild.Id, hexColor);
            await Response().Confirm(strs.welcomeimg_color_set(hexColor)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImgText([Leftover] string text)
        {
            await _service.SetTextAsync(ctx.Guild.Id, text);
            await Response().Confirm(strs.welcomeimg_text_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WelcomeImgTest()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Error(strs.welcomeimg_not_configured).SendAsync();
                return;
            }

            var user = ctx.User as Discord.WebSocket.SocketGuildUser;
            if (user is null) return;

            var imageStream = await _service.GenerateWelcomeImageAsync(config, user);
            if (imageStream is null)
            {
                await Response().Error(strs.welcomeimg_gen_failed).SendAsync();
                return;
            }

            await ctx.Channel.SendFileAsync(imageStream, "welcome_test.png",
                text: "Here's a preview of the welcome image:");

            await imageStream.DisposeAsync();
        }
    }
}
