#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("smartspam")]
    public partial class SmartSpamCommands : SantiModule<SmartSpamService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task SmartSpam(string toggle)
        {
            var enabled = toggle.ToLowerInvariant() is "on" or "enable" or "true";
            await _service.SetConfigAsync(ctx.Guild.Id, enabled);

            if (enabled)
                await Response().Confirm("Smart spam detection **enabled**.").SendAsync();
            else
                await Response().Confirm("Smart spam detection **disabled**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task SmartSpamThreshold(int threshold)
        {
            if (threshold < 1 || threshold > 100)
            {
                await Response().Error("Threshold must be between 1 and 100.").SendAsync();
                return;
            }

            await _service.SetConfigAsync(ctx.Guild.Id, true, threshold: threshold);
            await Response().Confirm($"Smart spam threshold set to **{threshold}**/100.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task SmartSpamAction([Leftover] string action)
        {
            var validActions = new[] { "delete", "warn", "mute" };
            if (!validActions.Contains(action.ToLowerInvariant()))
            {
                await Response().Error("Invalid action. Use: delete, warn, mute").SendAsync();
                return;
            }

            await _service.SetConfigAsync(ctx.Guild.Id, true, action: action.ToLowerInvariant());
            await Response().Confirm($"Smart spam action set to **{action}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task SmartSpamInfo()
        {
            var config = _service.GetConfig(ctx.Guild.Id);
            if (config is null)
            {
                await Response().Error("Smart spam is not configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Smart Spam Detection")
                .AddField("Status", config.IsEnabled ? "Enabled" : "Disabled", true)
                .AddField("Threshold", $"{config.Threshold}/100", true)
                .AddField("Action", config.Action, true)
                .WithDescription("Scores messages based on: frequency, caps ratio, emoji density, link density, duplicate text")
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
