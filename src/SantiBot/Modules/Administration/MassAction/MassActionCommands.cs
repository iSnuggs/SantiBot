#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("mass")]
    public partial class MassActionCommands : SantiModule<MassActionService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task MassBan(IRole role, [Leftover] string reason = "Mass ban")
        {
            var embed = CreateEmbed()
                .WithDescription($"⚠️ **DANGEROUS** - This will ban ALL members with the **{role.Name}** role.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();

            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Mass ban cancelled.").SendAsync();
                return;
            }

            var count = await _service.MassBanByRoleAsync(ctx.Guild, role, reason);
            await Response().Confirm($"Mass banned **{count}** members with role **{role.Name}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [UserPerm(GuildPerm.KickMembers)]
        public async Task MassKick(IRole role, [Leftover] string reason = "Mass kick")
        {
            var embed = CreateEmbed()
                .WithDescription($"⚠️ **DANGEROUS** - This will kick ALL members with the **{role.Name}** role.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();

            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Mass kick cancelled.").SendAsync();
                return;
            }

            var count = await _service.MassKickByRoleAsync(ctx.Guild, role, reason);
            await Response().Confirm($"Mass kicked **{count}** members with role **{role.Name}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        [UserPerm(GuildPerm.ModerateMembers)]
        public async Task MassMute(IRole role, int minutes = 10)
        {
            var embed = CreateEmbed()
                .WithDescription($"⚠️ **DANGEROUS** - This will timeout ALL members with the **{role.Name}** role for {minutes} minutes.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();

            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Mass mute cancelled.").SendAsync();
                return;
            }

            var count = await _service.MassMuteByRoleAsync(ctx.Guild, role, TimeSpan.FromMinutes(minutes));
            await Response().Confirm($"Mass muted **{count}** members with role **{role.Name}** for **{minutes}** minutes.").SendAsync();
        }
    }
}
