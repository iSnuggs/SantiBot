namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("RssManager")]
    [Group("rss")]
    public partial class RssManagerCommands : SantiModule<RssManager.RssManagerService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RssAdd(string url, ITextChannel channel = null!)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                await Response().Error("Invalid URL.").SendAsync();
                return;
            }

            channel ??= (ITextChannel)ctx.Channel;
            var id = await _service.AddFeedAsync(ctx.Guild.Id, channel.Id, url);
            if (id < 0)
            {
                await Response().Error("That feed is already added.").SendAsync();
                return;
            }

            await Response()
                .Confirm($"RSS feed #{id} added to {channel.Mention}")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RssRemove(int id)
        {
            var success = await _service.RemoveFeedAsync(ctx.Guild.Id, id);
            if (success)
                await Response().Confirm($"RSS feed #{id} removed.").SendAsync();
            else
                await Response().Error("Feed not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task RssList()
        {
            var feeds = await _service.ListFeedsAsync(ctx.Guild.Id);
            if (feeds.Count == 0)
            {
                await Response().Error("No RSS feeds configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", feeds.Select(f =>
                $"`#{f.Id}` {f.Url.TrimTo(60)} -> <#{f.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("RSS Feeds").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task RssTest(string url)
        {
            await ctx.Channel.TriggerTypingAsync();
            var (success, title) = await _service.TestFeedAsync(url);
            if (success)
                await Response().Confirm($"Valid feed: **{title}**").SendAsync();
            else
                await Response().Error("Could not parse that URL as RSS/Atom.").SendAsync();
        }
    }
}
