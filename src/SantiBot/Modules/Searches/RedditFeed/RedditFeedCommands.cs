namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("RedditFeed")]
    [Group("reddit")]
    public partial class RedditFeedCommands : SantiModule<RedditFeed.RedditFeedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RedditFollow(string subreddit, ITextChannel channel = null)
        {
            channel ??= (ITextChannel)ctx.Channel;
            var success = await _service.FollowAsync(ctx.Guild.Id, channel.Id, subreddit);
            if (success)
                await Response()
                    .Confirm($"Now following **r/{subreddit.TrimStart('/')}** in {channel.Mention}")
                    .SendAsync();
            else
                await Response().Error("Already following that subreddit.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RedditUnfollow(string subreddit)
        {
            var success = await _service.UnfollowAsync(ctx.Guild.Id, subreddit);
            if (success)
                await Response().Confirm($"Unfollowed **r/{subreddit}**").SendAsync();
            else
                await Response().Error("Not following that subreddit.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RedditList()
        {
            var follows = await _service.ListAsync(ctx.Guild.Id);
            if (follows.Count == 0)
            {
                await Response().Error("No Reddit feeds configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", follows.Select((f, i) =>
                $"`{i + 1}.` r/{f.Subreddit} -> <#{f.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Reddit Feeds").WithDescription(desc))
                .SendAsync();
        }
    }
}
