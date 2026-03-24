#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class TikTokCommands : SantiModule<TikTokFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TikTokFollow(string username)
        {
            var (success, error, feedUrl) = await _service.SubscribeAsync(
                ctx.Guild.Id, ctx.Channel.Id, username);

            if (success)
                await Response().Confirm(strs.tiktok_followed(username, ctx.Channel.Id)).SendAsync();
            else
                await Response().Error(strs.tiktok_follow_failed(error)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TikTokUnfollow(string username)
        {
            var removed = await _service.UnsubscribeAsync(ctx.Guild.Id, ctx.Channel.Id, username);

            if (removed)
                await Response().Confirm(strs.tiktok_unfollowed(username)).SendAsync();
            else
                await Response().Error(strs.tiktok_not_following(username)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TikTokList()
        {
            var feeds = await _service.GetTikTokFeedsAsync(ctx.Guild.Id);

            if (feeds.Count == 0)
            {
                await Response().Confirm(strs.tiktok_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("TikTok Feeds")
                .WithOkColor();

            foreach (var feed in feeds)
            {
                // Extract username from URL
                var username = feed.Url.Split("/@").LastOrDefault() ?? feed.Url;
                embed.AddField($"@{username}", $"Channel: <#{feed.ChannelId}>", true);
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
