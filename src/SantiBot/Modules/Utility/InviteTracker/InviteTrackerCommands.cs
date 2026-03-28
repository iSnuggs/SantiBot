namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("InviteTracker")]
    [Group("invites")]
    public partial class InviteTrackerCommands : SantiModule<InviteTrackerService>
    {
        [Cmd]
        [Priority(0)]
        public async Task Invites()
        {
            var count = await _service.GetInviteCountAsync(ctx.Guild.Id, ctx.User.Id);

            await Response()
                .Confirm(strs.invite_count(ctx.User.Mention, count))
                .SendAsync();
        }

        [Cmd]
        [Priority(1)]
        public async Task Invites(IUser target)
        {
            var count = await _service.GetInviteCountAsync(ctx.Guild.Id, target.Id);

            await Response()
                .Confirm(strs.invite_count(target.Mention, count))
                .SendAsync();
        }

        [Cmd]
        public async Task InvitesLeaderboard()
        {
            var leaders = await _service.GetLeaderboardAsync(ctx.Guild.Id);

            if (leaders.Count == 0)
            {
                await Response().Error(strs.invite_nobody).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.invite_leaderboard));

            for (var i = 0; i < leaders.Count; i++)
            {
                var (userId, count) = leaders[i];
                eb.AddField($"#{i + 1}", $"<@{userId}> — **{count}** invites", false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvitesEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.invite_enabled).SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvitesDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.invite_disabled).SendAsync();
        }

        [Cmd]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvitesLog(ITextChannel channel)
        {
            await _service.SetLogChannelAsync(ctx.Guild.Id, channel.Id);
            await Response()
                .Confirm(strs.invite_log_set(channel.Mention))
                .SendAsync();
        }
    }
}
