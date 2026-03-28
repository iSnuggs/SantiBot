namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Counting")]
    [Group("counting")]
    public partial class CountingCommands : SantiModule<CountingService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Priority(2)]
        public async Task Counting(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.counting_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [Priority(1)]
        public async Task CountingReset()
        {
            var success = await _service.ResetAsync(ctx.Guild.Id);

            if (!success)
            {
                await Response().Error(strs.counting_not_configured).SendAsync();
                return;
            }

            await Response().Confirm(strs.counting_reset).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Counting()
        {
            var status = await _service.GetStatusAsync(ctx.Guild.Id);

            if (status is null || !status.Enabled)
            {
                await Response().Error(strs.counting_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.counting_status_title))
                .AddField(GetText(strs.counting_current), status.CurrentCount.ToString(), true)
                .AddField(GetText(strs.counting_channel), $"<#{status.ChannelId}>", true);

            if (status.LastCountUserId != 0)
                eb.AddField(GetText(strs.counting_last_user), $"<@{status.LastCountUserId}>", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
