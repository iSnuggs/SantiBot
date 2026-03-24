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
            var (success, error) = await _service.FollowAsync(ctx.Guild.Id, ctx.Channel.Id, username);

            if (success)
                await Response().Confirm(strs.tiktok_followed(
                    TikTokFeedService.ParseUsername(username), ctx.Channel.Id)).SendAsync();
            else
                await Response().Error(strs.tiktok_follow_failed(error)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TikTokUnfollow(string username)
        {
            var removed = await _service.UnfollowAsync(ctx.Guild.Id, ctx.Channel.Id, username);

            if (removed)
                await Response().Confirm(strs.tiktok_unfollowed(
                    TikTokFeedService.ParseUsername(username))).SendAsync();
            else
                await Response().Error(strs.tiktok_not_following(
                    TikTokFeedService.ParseUsername(username) ?? username)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TikTokList()
        {
            var follows = await _service.GetFollowsAsync(ctx.Guild.Id);

            if (follows.Count == 0)
            {
                await Response().Confirm(strs.tiktok_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("🎵 TikTok Feeds")
                .WithColor(new Discord.Color(0xFF0050))
                .WithOkColor();

            foreach (var follow in follows)
                embed.AddField($"@{follow.Username}", $"Channel: <#{follow.ChannelId}>", true);

            await Response().Embed(embed).SendAsync();
        }
    }
}
