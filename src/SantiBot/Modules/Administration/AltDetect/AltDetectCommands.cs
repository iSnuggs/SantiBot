namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("altdetect")]
    [Name("AltDetect")]
    public partial class AltDetectCommands : SantiModule<AltDetectService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AltDetectEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.altdetect_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AltDetectDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.altdetect_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AltDetectAge(int days)
        {
            if (days < 0 || days > 365)
            {
                await Response().Error(strs.altdetect_age_invalid).SendAsync();
                return;
            }

            await _service.SetMinAgeAsync(ctx.Guild.Id, days);
            await Response().Confirm(strs.altdetect_age_set(days)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AltDetectAction([Leftover] string action)
        {
            var valid = new[] { "alert", "kick", "ban" };
            if (!valid.Contains(action.ToLowerInvariant()))
            {
                await Response().Error(strs.altdetect_action_invalid).SendAsync();
                return;
            }

            await _service.SetActionAsync(ctx.Guild.Id, action);
            await Response().Confirm(strs.altdetect_action_set(action)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AltDetectChannel(ITextChannel channel)
        {
            await _service.SetAlertChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.altdetect_channel_set(channel.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AltDetectLink(IGuildUser mainUser, IGuildUser altUser, [Leftover] string reason = "Manually linked")
        {
            await _service.MarkAltAsync(ctx.Guild.Id, mainUser.Id, altUser.Id, reason);
            await Response().Confirm(strs.altdetect_linked(altUser.Mention, mainUser.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AltDetectCheck(IGuildUser user)
        {
            var alts = await _service.GetAltsAsync(ctx.Guild.Id, user.Id);

            if (alts.Count == 0)
            {
                await Response().Confirm(strs.altdetect_no_alts(user.Mention)).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Known Alts for {user.Username}");

            foreach (var alt in alts)
            {
                var otherId = alt.MainUserId == user.Id ? alt.AltUserId : alt.MainUserId;
                var role = alt.MainUserId == user.Id ? "Alt" : "Main";
                eb.AddField($"{role}: <@{otherId}>",
                    $"Reason: {alt.Reason}\nDetected: {alt.DetectedAt:yyyy-MM-dd}",
                    true);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
