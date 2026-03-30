namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("XFeed")]
    [Group("xfeed")]
    public partial class XFeedCommands : SantiModule<XFeed.XFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task XFeedFollow(string handle, ITextChannel channel = null!)
        {
            channel ??= (ITextChannel)ctx.Channel;
            handle = handle.TrimStart('@');
            var success = await _service.FollowAsync(ctx.Guild.Id, channel.Id, handle);
            if (success)
                await Response()
                    .Confirm($"Now following **@{handle}** in {channel.Mention}")
                    .SendAsync();
            else
                await Response().Error("Already following that account.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task XFeedUnfollow(string handle)
        {
            var success = await _service.UnfollowAsync(ctx.Guild.Id, handle);
            if (success)
                await Response().Confirm($"Unfollowed **@{handle}**").SendAsync();
            else
                await Response().Error("Not following that account.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task XFeedList()
        {
            var follows = await _service.ListAsync(ctx.Guild.Id);
            if (follows.Count == 0)
            {
                await Response().Error("No X/Twitter feeds configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", follows.Select((f, i) =>
                $"`{i + 1}.` @{f.Handle} -> <#{f.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("X/Twitter Feeds").WithDescription(desc))
                .SendAsync();
        }
    }
}
