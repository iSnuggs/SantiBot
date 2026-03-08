#nullable disable
using NadekoBot.Modules.Administration.Services;

namespace NadekoBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AnnouncementCommands(AutoPublishService autoPubService) : NadekoModule
    {
        [Cmd]
        [UserPerm(ChannelPerm.ManageMessages)]
        public async Task AutoPublish()
        {
            if (ctx.Channel.GetChannelType() != ChannelType.News)
            {
                await Response().Error(strs.req_announcement_channel).SendAsync();
                return;
            }

            var result = await autoPubService.ToggleAutoPublish(ctx.Guild.Id, ctx.Channel.Id);

            if (result)
            {
                await Response().Confirm(strs.autopublish_enable).SendAsync();
            }
            else
            {
                await Response().Confirm(strs.autopublish_disable).SendAsync();
            }
        }
    }
}
