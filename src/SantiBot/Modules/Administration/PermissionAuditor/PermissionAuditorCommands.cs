#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("permaudit")]
    public partial class PermissionAuditorCommands : SantiModule<PermissionAuditorService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task PermAudit()
        {
            var result = await _service.AuditGuildAsync(ctx.Guild);

            var embed = CreateEmbed()
                .WithTitle("Permission Audit Report")
                .WithOkColor();

            if (result.DangerousRoles.Count > 0)
            {
                var roleLines = result.DangerousRoles
                    .Take(10)
                    .Select(r => $"**{r.RoleName}**: {string.Join(", ", r.DangerousPerms)}");
                embed.AddField("Roles with Dangerous Permissions", string.Join("\n", roleLines));
            }
            else
            {
                embed.AddField("Roles with Dangerous Permissions", "None found - looking good!");
            }

            if (result.Warnings.Count > 0)
                embed.AddField("Warnings", string.Join("\n", result.Warnings.Select(w => $"⚠️ {w}")));

            if (result.Conflicts.Count > 0)
                embed.AddField("Permission Conflicts", string.Join("\n", result.Conflicts.Take(10)));

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task PermAudit(ITextChannel channel)
        {
            var result = await _service.AuditChannelAsync(channel, ctx.Guild);

            var embed = CreateEmbed()
                .WithTitle($"Permission Audit: #{channel.Name}")
                .WithOkColor();

            if (result.ChannelOverwrites.Count == 0)
            {
                embed.WithDescription("No permission overwrites on this channel.");
            }
            else
            {
                foreach (var ow in result.ChannelOverwrites.Take(15))
                {
                    var desc = "";
                    if (ow.Allows.Count > 0) desc += $"✅ {string.Join(", ", ow.Allows)}\n";
                    if (ow.Denies.Count > 0) desc += $"❌ {string.Join(", ", ow.Denies)}";
                    embed.AddField($"{ow.TargetType}: {ow.TargetName}", desc.Length > 0 ? desc : "No overrides");
                }
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task PermAudit(IRole role)
        {
            var result = _service.AuditRole(role, ctx.Guild);

            var embed = CreateEmbed()
                .WithTitle($"Permission Audit: @{role.Name}")
                .WithDescription(result.RolePermissions.Count > 0
                    ? string.Join(", ", result.RolePermissions)
                    : "No permissions")
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
