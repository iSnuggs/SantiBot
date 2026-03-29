#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("rolesuite")]
    public partial class RoleSuiteCommands : SantiModule<RoleSuiteService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleSuiteCreate(string name, string color = null)
        {
            try
            {
                var role = await _service.CreateRoleAsync(ctx.Guild, name, color);
                await Response().Confirm($"Role **{role.Name}** created.{(color is not null ? $" Color: {color}" : "")}").SendAsync();
            }
            catch (Exception ex)
            {
                await Response().Error($"Failed to create role: {ex.Message}").SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleSuiteDelete(IRole role)
        {
            if (!await CheckRoleHierarchy(role))
                return;

            if (await _service.DeleteRoleAsync(ctx.Guild, role))
                await Response().Confirm($"Role **{role.Name}** deleted.").SendAsync();
            else
                await Response().Error("Failed to delete role.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleSuiteRecolor(IRole role, string color)
        {
            if (!await CheckRoleHierarchy(role))
                return;

            if (await _service.RecolorRoleAsync(role, color))
                await Response().Confirm($"Role **{role.Name}** recolored to **{color}**.").SendAsync();
            else
                await Response().Error("Invalid color. Use hex format: #FF5733 or FF5733").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleSuiteInfo(IRole role)
        {
            var info = _service.GetRoleInfo(role, ctx.Guild);

            var embed = CreateEmbed()
                .WithTitle($"Role Info: {info.Name}")
                .AddField("ID", info.Id.ToString(), true)
                .AddField("Color", info.Color, true)
                .AddField("Position", info.Position.ToString(), true)
                .AddField("Hoisted", info.IsHoisted ? "Yes" : "No", true)
                .AddField("Mentionable", info.IsMentionable ? "Yes" : "No", true)
                .AddField("Managed", info.IsManaged ? "Yes" : "No", true)
                .AddField("Created", $"{info.CreatedAt:g}", true)
                .WithColor(role.Color.RawValue != 0 ? role.Color : Color.Default)
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleSuiteMembers(IRole role)
        {
            var members = await _service.GetRoleMembersAsync(ctx.Guild, role);

            var embed = CreateEmbed()
                .WithTitle($"Members with @{role.Name} ({members.Count})")
                .WithOkColor();

            if (members.Count == 0)
            {
                embed.WithDescription("No members have this role.");
            }
            else
            {
                var memberList = string.Join(", ", members.Take(50).Select(m => m.Mention));
                embed.WithDescription(memberList);
                if (members.Count > 50)
                    embed.WithFooter($"...and {members.Count - 50} more");
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
