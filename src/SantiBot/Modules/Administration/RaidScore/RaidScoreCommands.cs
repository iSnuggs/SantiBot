namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("raidscore")]
    [Name("RaidScore")]
    public partial class RaidScoreCommands : SantiModule<RaidScoreService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RaidScoreEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.raidscore_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RaidScoreDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.raidscore_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RaidScoreThreshold(int threshold)
        {
            if (threshold < 1 || threshold > 100)
            {
                await Response().Error(strs.raidscore_threshold_invalid).SendAsync();
                return;
            }

            await _service.SetThresholdAsync(ctx.Guild.Id, threshold);
            await Response().Confirm(strs.raidscore_threshold_set(threshold)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RaidScoreAction([Leftover] string action)
        {
            var valid = new[] { "alert", "quarantine", "kick", "ban" };
            if (!valid.Contains(action.ToLowerInvariant()))
            {
                await Response().Error(strs.raidscore_action_invalid).SendAsync();
                return;
            }

            await _service.SetActionAsync(ctx.Guild.Id, action);
            await Response().Confirm(strs.raidscore_action_set(action)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task RaidScoreChannel(ITextChannel channel)
        {
            await _service.SetAlertChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.raidscore_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task RaidScoreCheck(IGuildUser user)
        {
            if (user is not SocketGuildUser socketUser)
            {
                await Response().Error(strs.raidscore_user_not_found).SendAsync();
                return;
            }

            var score = _service.CalculateScore(socketUser, ctx.Guild.Id);
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            var threshold = config?.ThresholdScore ?? 70;

            var eb = CreateEmbed()
                .WithTitle($"Raid Score: {user.Username}")
                .WithDescription($"**Score: {score}/100**")
                .AddField("Account Age", $"{(DateTimeOffset.UtcNow - user.CreatedAt).TotalDays:F1} days", true)
                .AddField("Has Avatar", user.GetAvatarUrl() is not null ? "Yes" : "No", true)
                .AddField("Threshold", threshold.ToString(), true)
                .WithColor(score >= threshold ? Color.Red : score >= 40 ? Color.Orange : Color.Green);

            await Response().Embed(eb).SendAsync();
        }
    }
}
