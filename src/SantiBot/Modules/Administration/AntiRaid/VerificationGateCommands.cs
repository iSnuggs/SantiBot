#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class VerificationGateCommands : SantiModule<VerificationGateService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VerifyEnable()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            var newState = config is null || !config.Enabled;
            await _service.EnableAsync(ctx.Guild.Id, newState);
            await Response().Confirm(strs.verify_toggled(newState ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VerifyRole(IRole role)
        {
            await _service.SetVerifiedRoleAsync(ctx.Guild.Id, role.Id);
            await Response().Confirm(strs.verify_role_set(role.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VerifyMessage([Leftover] string message)
        {
            await _service.SetVerifyMessageAsync(ctx.Guild.Id, message);
            await Response().Confirm(strs.verify_message_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VerifyPanel()
        {
            var success = await _service.SendVerifyPanelAsync(ctx.Guild.Id, (ITextChannel)ctx.Channel);

            if (!success)
                await Response().Error(strs.verify_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task Lockdown(string state = null)
        {
            if (state?.ToLowerInvariant() == "off")
            {
                await _service.SetLockdownAsync(ctx.Guild.Id, false);

                // Reset verification level to medium
                try
                {
                    var guild = ctx.Guild as Discord.WebSocket.SocketGuild;
                    if (guild is not null)
                        await guild.ModifyAsync(g => g.VerificationLevel = VerificationLevel.Medium);
                }
                catch { }

                await Response().Confirm(strs.lockdown_disabled).SendAsync();
            }
            else
            {
                await _service.SetLockdownAsync(ctx.Guild.Id, true);

                try
                {
                    var guild = ctx.Guild as Discord.WebSocket.SocketGuild;
                    if (guild is not null)
                        await guild.ModifyAsync(g => g.VerificationLevel = VerificationLevel.Extreme);
                }
                catch { }

                await Response().Confirm(strs.lockdown_enabled).SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoLockdown(int joinThreshold = 10, int timeWindowSeconds = 10)
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            var newState = config is null || !config.AutoLockdownEnabled;

            await _service.SetAutoLockdownAsync(ctx.Guild.Id, newState, joinThreshold, timeWindowSeconds);

            if (newState)
                await Response().Confirm(strs.autolockdown_enabled(joinThreshold, timeWindowSeconds)).SendAsync();
            else
                await Response().Confirm(strs.autolockdown_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task MassJoinBan(int seconds = 60)
        {
            if (seconds < 10 || seconds > 3600)
            {
                await Response().Error(strs.massjoinban_invalid_time).SendAsync();
                return;
            }

            var count = await _service.MassJoinBanAsync(ctx.Guild.Id, seconds);
            await Response().Confirm(strs.massjoinban_result(count, seconds)).SendAsync();
        }
    }
}
