namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("Confession")]
    [Group("confession")]
    public partial class ConfessionCommands : SantiModule<ConfessionService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Confess([Leftover] string text)
        {
            // Delete the command message to keep the confession anonymous
            try
            {
                await ctx.Message.DeleteAsync();
            }
            catch { }

            if (string.IsNullOrWhiteSpace(text))
            {
                // Send error as DM since original message is deleted
                try
                {
                    var dm = await ctx.User.CreateDMChannelAsync();
                    await dm.SendMessageAsync(GetText(strs.confession_empty));
                }
                catch { }
                return;
            }

            var result = await _service.SubmitAsync(ctx.Guild.Id, ctx.User.Id, text);

            if (result is null)
            {
                // Send error as DM since original message is deleted
                try
                {
                    var dm = await ctx.User.CreateDMChannelAsync();
                    await dm.SendMessageAsync(GetText(strs.confession_not_enabled));
                }
                catch { }
                return;
            }

            // Confirm via DM
            try
            {
                var dm = await ctx.User.CreateDMChannelAsync();
                await dm.SendMessageAsync(GetText(strs.confession_submitted(result.Value)));
            }
            catch { }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ConfessionChannel(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.confession_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ConfessionEnable()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null || config.ChannelId == 0)
            {
                await Response().Error(strs.confession_no_channel).SendAsync();
                return;
            }

            await _service.EnableAsync(ctx.Guild.Id, config.ChannelId);
            await Response().Confirm(strs.confession_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ConfessionDisable()
        {
            var success = await _service.DisableAsync(ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(strs.confession_not_configured).SendAsync();
                return;
            }

            await Response().Confirm(strs.confession_disabled).SendAsync();
        }
    }
}
