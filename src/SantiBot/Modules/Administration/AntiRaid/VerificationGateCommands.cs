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

                // Restore previous verification level (saved before lockdown)
                try
                {
                    var guild = ctx.Guild as Discord.WebSocket.SocketGuild;
                    if (guild is not null)
                    {
                        var previousLevel = _service.GetPreviousVerificationLevel(guild.Id);
                        await guild.ModifyAsync(g => g.VerificationLevel = previousLevel);
                    }
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
        [UserPerm(GuildPerm.Administrator)]
        public async Task VerifySetup(IRole verifiedRole, ITextChannel verifyChannel)
        {
            // 1. Set role and channel
            await _service.SetVerifiedRoleAsync(ctx.Guild.Id, verifiedRole.Id);
            await _service.SetVerifyChannelAsync(ctx.Guild.Id, verifyChannel.Id);
            await _service.EnableAsync(ctx.Guild.Id, true);

            // 2. Lock @everyone out of all text channels EXCEPT the verify channel
            var guild = (SocketGuild)ctx.Guild;
            var everyoneRole = guild.EveryoneRole;
            var lockedCount = 0;

            foreach (var channel in guild.TextChannels)
            {
                try
                {
                    if (channel.Id == verifyChannel.Id)
                    {
                        // Verify channel: everyone can read, but only send after verified
                        await channel.AddPermissionOverwriteAsync(everyoneRole,
                            new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny));
                        await channel.AddPermissionOverwriteAsync(verifiedRole,
                            new OverwritePermissions(sendMessages: PermValue.Allow));
                    }
                    else
                    {
                        // All other channels: deny view for everyone, allow for verified
                        await channel.AddPermissionOverwriteAsync(everyoneRole,
                            new OverwritePermissions(viewChannel: PermValue.Deny));
                        await channel.AddPermissionOverwriteAsync(verifiedRole,
                            new OverwritePermissions(viewChannel: PermValue.Allow));
                        lockedCount++;
                    }
                }
                catch { /* bot may lack permissions on some channels */ }
            }

            // 3. Send the verify panel
            await _service.SendVerifyPanelAsync(ctx.Guild.Id, verifyChannel);

            await Response().Confirm(
                $"✅ **Verification gate is live!**\n\n" +
                $"Verified role: {verifiedRole.Mention}\n" +
                $"Verify channel: {verifyChannel.Mention}\n" +
                $"Channels locked: **{lockedCount}**\n\n" +
                $"New members must click the verify button in {verifyChannel.Mention} to access the server."
            ).SendAsync();
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
