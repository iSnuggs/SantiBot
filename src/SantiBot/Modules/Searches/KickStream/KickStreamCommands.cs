#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class KickStreamCommands : SantiModule<KickStreamService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task KickFollow(string username, ITextChannel channel = null)
        {
            channel ??= (ITextChannel)ctx.Channel;

            var (success, error) = await _service.FollowAsync(ctx.Guild.Id, channel.Id, username);

            if (success)
                await Response().Confirm(strs.kick_followed(username, channel.Id)).SendAsync();
            else
                await Response().Error(strs.kick_follow_failed(error)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task KickUnfollow(string username)
        {
            var removed = await _service.UnfollowAsync(ctx.Guild.Id, username);

            if (removed)
                await Response().Confirm(strs.kick_unfollowed(username)).SendAsync();
            else
                await Response().Error(strs.kick_not_following(username)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task KickList()
        {
            var follows = await _service.GetFollowsAsync(ctx.Guild.Id);

            if (follows.Count == 0)
            {
                await Response().Confirm(strs.kick_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("🟢 Kick Stream Follows")
                .WithColor(new Discord.Color(0x53FC18))
                .WithOkColor();

            foreach (var follow in follows)
            {
                var status = follow.IsLive ? "🔴 LIVE" : "⚫ Offline";
                embed.AddField(
                    $"{follow.KickUsername} ({status})",
                    $"Notify: <#{follow.NotifyChannelId}>",
                    true);
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
