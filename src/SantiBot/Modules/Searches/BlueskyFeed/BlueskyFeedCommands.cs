#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class BlueskyFeedCommands : SantiModule<BlueskyFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BskyFollow(string handle, ITextChannel channel = null)
        {
            channel ??= (ITextChannel)ctx.Channel;

            var (success, result) = await _service.FollowAsync(ctx.Guild.Id, channel.Id, handle);

            if (success)
                await Response().Confirm(strs.bsky_followed(result, channel.Id)).SendAsync();
            else
                await Response().Error(strs.bsky_follow_failed(result)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BskyUnfollow(string handle)
        {
            var removed = await _service.UnfollowAsync(ctx.Guild.Id, handle);

            if (removed)
                await Response().Confirm(strs.bsky_unfollowed(handle)).SendAsync();
            else
                await Response().Error(strs.bsky_not_following(handle)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BskyList()
        {
            var subs = await _service.GetSubsAsync(ctx.Guild.Id);

            if (subs.Count == 0)
            {
                await Response().Confirm(strs.bsky_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("🦋 Bluesky Feeds")
                .WithColor(new Discord.Color(0x0085FF))
                .WithOkColor();

            foreach (var sub in subs)
                embed.AddField(
                    sub.BlueskyHandle,
                    $"Channel: <#{sub.ChannelId}>",
                    true);

            await Response().Embed(embed).SendAsync();
        }
    }
}
