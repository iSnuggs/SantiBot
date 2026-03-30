namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("SportsScores")]
    [Group("sports")]
    public partial class SportsCommands : SantiModule<SportsScores.SportsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SportsFollow(string league, ITextChannel channel = null!)
        {
            channel ??= (ITextChannel)ctx.Channel;
            var success = await _service.FollowAsync(ctx.Guild.Id, channel.Id, league);
            if (success)
                await Response()
                    .Confirm($"Now following **{league.ToUpper()}** scores in {channel.Mention}")
                    .SendAsync();
            else
                await Response().Error("Already following that league, or unknown league. Supported: NFL, NBA, EPL, Soccer, MLB, NHL").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SportsScores(string league)
        {
            await ctx.Channel.TriggerTypingAsync();
            var scores = await _service.GetScoresAsync(league);
            await Response()
                .Embed(CreateEmbed().WithOkColor()
                    .WithTitle($"{league.ToUpper()} Scores")
                    .WithDescription(scores))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task SportsUnfollow(string league)
        {
            var success = await _service.UnfollowAsync(ctx.Guild.Id, league);
            if (success)
                await Response().Confirm($"Unfollowed **{league.ToUpper()}**").SendAsync();
            else
                await Response().Error("Not following that league.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SportsList()
        {
            var follows = await _service.ListAsync(ctx.Guild.Id);
            if (follows.Count == 0)
            {
                await Response().Error("No sports feeds configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", follows.Select((f, i) =>
                $"`{i + 1}.` {f.League.ToUpper()} -> <#{f.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Sports Feeds").WithDescription(desc))
                .SendAsync();
        }
    }
}
