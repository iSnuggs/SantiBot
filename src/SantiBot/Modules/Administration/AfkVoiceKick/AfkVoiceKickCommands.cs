namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("afkkick")]
    [Name("AfkVoiceKick")]
    public partial class AfkVoiceKickCommands : SantiModule<AfkVoiceKickService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKick()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Error(strs.afkkick_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("AFK Voice Kick Configuration")
                .AddField("Enabled", config.Enabled ? "Yes" : "No", true)
                .AddField("Idle Minutes", config.IdleMinutes.ToString(), true)
                .AddField("Exempt Role", config.ExemptRoleId is { } rid ? $"<@&{rid}>" : "None", true)
                .AddField("AFK Channel", config.AfkChannelId is { } cid ? $"<#{cid}>" : "Disconnect", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKickEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id, true);
            await Response().Confirm(strs.afkkick_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKickDisable()
        {
            await _service.EnableAsync(ctx.Guild.Id, false);
            await Response().Confirm(strs.afkkick_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKickTime(int minutes)
        {
            if (minutes < 1 || minutes > 1440)
            {
                await Response().Error(strs.afkkick_time_invalid).SendAsync();
                return;
            }

            await _service.SetIdleMinutesAsync(ctx.Guild.Id, minutes);
            await Response().Confirm(strs.afkkick_time_set(minutes)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKickExempt(IRole role)
        {
            await _service.SetExemptRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.afkkick_exempt_set(role.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AfkKickChannel(IVoiceChannel channel)
        {
            await _service.SetAfkChannelAsync(ctx.Guild.Id, channel.Id);
            await Response().Confirm(strs.afkkick_channel_set(channel.Name)).SendAsync();
        }
    }
}
