namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("RoleHierarchy")]
    [Group("rolehierarchy")]
    public partial class RoleHierarchyCommands : SantiModule<DashboardApi.RoleHierarchy.RoleHierarchyService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task RoleHierarchyList()
        {
            var roles = _service.GetRoles(ctx.Guild.Id);
            if (roles.Count == 0)
            {
                await Response().Error("No roles found.").SendAsync();
                return;
            }

            var desc = string.Join("\n", roles.Take(25).Select(r =>
                $"`{r.Position}.` **{r.Name}** ({r.MemberCount} members) {(r.IsManaged ? "[managed]" : "")}"));

            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Role Hierarchy").WithDescription(desc))
                .SendAsync();
        }
    }
}
