namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("PermissionsMatrix")]
    [Group("permmatrix")]
    public partial class PermissionsMatrixCommands : SantiModule<DashboardApi.PermissionsMatrix.PermissionsMatrixService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task PermMatrixView()
        {
            var entries = await _service.GetMatrixAsync(ctx.Guild.Id);
            if (entries.Count == 0)
            {
                await Response().Confirm("No custom permission overrides configured. Use the dashboard for a visual matrix view.").SendAsync();
                return;
            }

            var desc = string.Join("\n", entries.Take(25).Select(e =>
                $"{(e.IsAllowed ? "+" : "-")} **{e.Command}** for `{e.RoleName}`"));

            await Response()
                .Embed(CreateEmbed().WithOkColor()
                    .WithTitle("Permissions Matrix")
                    .WithDescription(desc)
                    .WithFooter("Use the dashboard for the full visual grid"))
                .SendAsync();
        }
    }
}
