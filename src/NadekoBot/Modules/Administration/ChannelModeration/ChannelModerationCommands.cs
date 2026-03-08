#nullable disable
using NadekoBot.Common.TypeReaders.Models;
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ChannelModerationCommands : NadekoModule
    {
        private readonly SomethingOnlyChannelService _somethingOnly;

        public ChannelModerationCommands(SomethingOnlyChannelService somethingOnly)
        {
            _somethingOnly = somethingOnly;
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageGuild)]
        public async Task ImageOnlyChannel(ParsedTimespan timespan = null)
        {
            var newValue = await _somethingOnly.ToggleImageOnlyChannelAsync(ctx.Guild.Id, ctx.Channel.Id);
            if (newValue)
                await Response().Confirm(strs.imageonly_enable).SendAsync();
            else
                await Response().Pending(strs.imageonly_disable).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        [BotPerm(GuildPerm.ManageGuild)]
        public async Task LinkOnlyChannel(ParsedTimespan timespan = null)
        {
            var newValue = await _somethingOnly.ToggleLinkOnlyChannelAsync(ctx.Guild.Id, ctx.Channel.Id);
            if (newValue)
                await Response().Confirm(strs.linkonly_enable).SendAsync();
            else
                await Response().Pending(strs.linkonly_disable).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(ChannelPerm.ManageChannels)]
        [BotPerm(ChannelPerm.ManageChannels)]
        public async Task Slowmode(ParsedTimespan timespan = null)
        {
            var seconds = (int?)timespan?.Time.TotalSeconds ?? 0;
            if (timespan is not null && (timespan.Time < TimeSpan.FromSeconds(0) || timespan.Time > TimeSpan.FromHours(6)))
                return;

            await ((ITextChannel)ctx.Channel).ModifyAsync(tcp =>
            {
                tcp.SlowModeInterval = seconds;
            });

            await ctx.OkAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        [BotPerm(GuildPerm.ManageChannels)]
        public async Task AgeRestrictToggle()
        {
            var channel = (ITextChannel)ctx.Channel;
            var isEnabled = channel.IsNsfw;

            await channel.ModifyAsync(c => c.IsNsfw = !isEnabled);

            if (isEnabled)
                await Response().Confirm(strs.nsfw_set_false).SendAsync();
            else
                await Response().Confirm(strs.nsfw_set_true).SendAsync();
        }
    }
}
