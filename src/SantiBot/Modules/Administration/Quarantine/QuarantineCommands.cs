namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("quarantine")]
    [Name("Quarantine")]
    public partial class QuarantineCommands : SantiModule<QuarantineService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Quarantine()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Error(strs.quarantine_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Quarantine Configuration")
                .AddField("Status", config.Enabled ? "Enabled" : "Disabled", true)
                .AddField("Quarantine Role", config.QuarantineRoleId.HasValue ? $"<@&{config.QuarantineRoleId}>" : "Not set", true)
                .AddField("Min Account Age", $"{config.MinAccountAgeDays} days", true)
                .AddField("No Avatar Check", config.QuarantineNoAvatar ? "On" : "Off", true)
                .AddField("Log Channel", config.LogChannelId.HasValue ? $"<#{config.LogChannelId}>" : "Not set", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id);
            await Response().Confirm(strs.quarantine_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineDisable()
        {
            if (await _service.DisableAsync(ctx.Guild.Id))
                await Response().Confirm(strs.quarantine_disabled).SendAsync();
            else
                await Response().Error(strs.quarantine_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineRole(IRole role)
        {
            await _service.SetRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.quarantine_role_set(role.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineAge(int days)
        {
            if (days < 0 || days > 365)
            {
                await Response().Error(strs.quarantine_age_invalid).SendAsync();
                return;
            }

            if (await _service.SetMinAgeAsync(ctx.Guild.Id, days))
                await Response().Confirm(strs.quarantine_age_set(days)).SendAsync();
            else
                await Response().Error(strs.quarantine_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineNoAvatar(bool enabled)
        {
            if (await _service.SetNoAvatarAsync(ctx.Guild.Id, enabled))
                await Response().Confirm(strs.quarantine_noavatar_set(enabled ? "on" : "off")).SendAsync();
            else
                await Response().Error(strs.quarantine_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineLog(ITextChannel channel)
        {
            if (await _service.SetLogChannelAsync(ctx.Guild.Id, channel.Id))
                await Response().Confirm(strs.quarantine_log_set(channel.Mention)).SendAsync();
            else
                await Response().Error(strs.quarantine_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task QuarantineRelease(IGuildUser user)
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config?.QuarantineRoleId is null)
            {
                await Response().Error(strs.quarantine_not_configured).SendAsync();
                return;
            }

            var role = ctx.Guild.GetRole(config.QuarantineRoleId.Value);
            if (role is null)
            {
                await Response().Error(strs.quarantine_role_not_found).SendAsync();
                return;
            }

            await user.RemoveRoleAsync(role);
            await Response().Confirm(strs.quarantine_released(user.Mention)).SendAsync();
        }
    }
}
