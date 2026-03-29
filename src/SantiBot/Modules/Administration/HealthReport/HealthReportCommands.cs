#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("healthreport")]
    public partial class HealthReportCommands : SantiModule<HealthReportService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task HealthReport()
        {
            await Response().Confirm("Generating health report...").SendAsync();

            var embed = await _service.GenerateReportAsync(ctx.Guild);
            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task HealthReportAuto(string toggle)
        {
            var enabled = toggle.ToLowerInvariant() is "on" or "enable" or "true";
            await _service.ToggleAutoAsync(ctx.Guild.Id, enabled);

            if (enabled)
                await Response().Confirm("Weekly health report **enabled**. Server owner will receive a DM every 7 days.").SendAsync();
            else
                await Response().Confirm("Weekly health report **disabled**.").SendAsync();
        }
    }
}
