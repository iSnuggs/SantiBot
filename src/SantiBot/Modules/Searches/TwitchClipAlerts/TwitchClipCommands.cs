namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Name("TwitchClipAlerts")]
    [Group("twitchclip")]
    public partial class TwitchClipCommands : SantiModule<TwitchClipAlerts.TwitchClipService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TwitchClipFollow(string twitchChannel, ITextChannel channel = null!)
        {
            channel ??= (ITextChannel)ctx.Channel;
            var success = await _service.FollowAsync(ctx.Guild.Id, channel.Id, twitchChannel);
            if (success)
                await Response()
                    .Confirm($"Now watching clips from **{twitchChannel}** in {channel.Mention}")
                    .SendAsync();
            else
                await Response().Error("Already following that channel.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TwitchClipUnfollow(string twitchChannel)
        {
            var success = await _service.UnfollowAsync(ctx.Guild.Id, twitchChannel);
            if (success)
                await Response().Confirm($"Stopped watching clips from **{twitchChannel}**").SendAsync();
            else
                await Response().Error("Not following that channel.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TwitchClipList()
        {
            var follows = await _service.ListAsync(ctx.Guild.Id);
            if (follows.Count == 0)
            {
                await Response().Error("No Twitch clip alerts configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", follows.Select((f, i) =>
                $"`{i + 1}.` {f.TwitchChannel} -> <#{f.ChannelId}>"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Twitch Clip Alerts").WithDescription(desc))
                .SendAsync();
        }
    }
}
