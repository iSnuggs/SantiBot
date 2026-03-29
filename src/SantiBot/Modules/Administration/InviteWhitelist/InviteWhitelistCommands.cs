#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("invwl")]
    public partial class InviteWhitelistCommands : SantiModule<InviteWhitelistService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvWlAdd(ulong serverId)
        {
            if (await _service.AddAsync(ctx.Guild.Id, serverId))
                await Response().Confirm($"Server **{serverId}** added to invite whitelist.").SendAsync();
            else
                await Response().Error("Server is already whitelisted.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvWlDel(ulong serverId)
        {
            if (await _service.RemoveAsync(ctx.Guild.Id, serverId))
                await Response().Confirm($"Server **{serverId}** removed from invite whitelist.").SendAsync();
            else
                await Response().Error("Server not found in whitelist.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvWlList()
        {
            var list = await _service.ListAsync(ctx.Guild.Id);
            if (list.Count == 0)
            {
                await Response().Error("No servers whitelisted.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Invite Whitelist")
                .WithDescription(string.Join("\n", list.Select(x => $"• Server ID: `{x.AllowedServerId}`")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task InvWlToggle()
        {
            var enabled = await _service.ToggleAsync(ctx.Guild.Id);
            await Response().Confirm($"Invite whitelist is now **{(enabled ? "enabled" : "disabled")}**.").SendAsync();
        }
    }
}
