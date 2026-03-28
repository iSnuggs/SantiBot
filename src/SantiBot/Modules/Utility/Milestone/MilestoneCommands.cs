namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group("milestone")]
    [Name("Milestone")]
    public partial class MilestoneCommands : SantiModule<MilestoneService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Milestone(ITextChannel channel)
        {
            await _service.SetChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.milestone_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task MilestoneDisable()
        {
            await _service.DisableAsync(ctx.Guild.Id);
            await Response().Confirm(strs.milestone_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Milestone()
        {
            var guild = (SocketGuild)ctx.Guild;
            var memberCount = guild.MemberCount;
            var config = _service.GetConfig(ctx.Guild.Id);

            var nextMilestone = MilestoneService.GetNextMilestone(config?.LastMilestone ?? 0);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Server Milestones")
                .AddField("Current Members", memberCount.ToString("N0"), true)
                .AddField("Next Milestone", nextMilestone?.ToString("N0") ?? "All milestones reached!", true)
                .AddField("Tracking", config is { Enabled: true, ChannelId: not null }
                    ? $"Enabled in <#{config.ChannelId}>"
                    : "Disabled", true);

            if (config?.LastMilestone > 0)
                eb.AddField("Last Milestone Hit", config.LastMilestone.ToString("N0"), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
