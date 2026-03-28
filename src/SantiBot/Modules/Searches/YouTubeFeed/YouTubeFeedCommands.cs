#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class YouTubeFeedCommands : SantiModule<YouTubeFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task YtFeedAdd(string ytChannelId, ITextChannel channel = null)
        {
            channel ??= (ITextChannel)ctx.Channel;

            var (success, result) = await _service.FollowAsync(ctx.Guild.Id, channel.Id, ytChannelId);

            if (success)
                await Response().Confirm(strs.ytfeed_followed(result, channel.Id)).SendAsync();
            else
                await Response().Error(strs.ytfeed_follow_failed(result)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task YtFeedRemove(string ytChannelId)
        {
            var removed = await _service.UnfollowAsync(ctx.Guild.Id, ytChannelId);

            if (removed)
                await Response().Confirm(strs.ytfeed_unfollowed(ytChannelId)).SendAsync();
            else
                await Response().Error(strs.ytfeed_not_following(ytChannelId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task YtFeedList()
        {
            var subs = await _service.GetSubsAsync(ctx.Guild.Id);

            if (subs.Count == 0)
            {
                await Response().Confirm(strs.ytfeed_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("📺 YouTube Feeds")
                .WithColor(new Discord.Color(0xFF0000))
                .WithOkColor();

            foreach (var sub in subs)
                embed.AddField(
                    string.IsNullOrEmpty(sub.YouTubeChannelName) ? sub.YouTubeChannelId : sub.YouTubeChannelName,
                    $"Channel: <#{sub.ChannelId}>\nID: `{sub.YouTubeChannelId}`",
                    true);

            await Response().Embed(embed).SendAsync();
        }
    }
}
