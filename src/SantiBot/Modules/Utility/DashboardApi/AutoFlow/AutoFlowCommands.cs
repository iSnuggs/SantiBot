namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("AutoFlow")]
    [Group("autoflow")]
    public partial class AutoFlowCommands : SantiModule<DashboardApi.AutoFlow.AutoFlowService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AutoFlowCreate([Leftover] string name)
        {
            var id = await _service.CreateFlowAsync(ctx.Guild.Id, name);
            await Response()
                .Confirm($"AutoFlow **{name}** (#{id}) created. Configure the trigger and response in the dashboard.")
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AutoFlowList()
        {
            var flows = await _service.ListFlowsAsync(ctx.Guild.Id);
            if (flows.Count == 0)
            {
                await Response().Error("No autoflows configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", flows.Select((f, i) =>
                $"`{i + 1}.` **{f.Name}** {(f.IsEnabled ? "Enabled" : "Disabled")}"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("AutoFlows").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task AutoFlowDelete([Leftover] string name)
        {
            var success = await _service.DeleteFlowAsync(ctx.Guild.Id, name);
            if (success)
                await Response().Confirm($"AutoFlow **{name}** deleted.").SendAsync();
            else
                await Response().Error("AutoFlow not found.").SendAsync();
        }
    }
}
