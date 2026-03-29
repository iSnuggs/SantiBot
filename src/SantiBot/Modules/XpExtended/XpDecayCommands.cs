#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("XpDecay")]
    [Group("xpdecay")]
    public partial class XpDecayCommands : SantiModule<XpDecayService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task XpdecayToggle(string toggle)
        {
            var enabled = toggle.ToLowerInvariant() switch
            {
                "on" or "enable" => true,
                "off" or "disable" => false,
                _ => (bool?)null
            };

            if (enabled is null)
            {
                await Response().Error("Use `on` or `off`.").SendAsync();
                return;
            }

            await _service.SetEnabledAsync(ctx.Guild.Id, enabled.Value);
            await Response().Confirm(
                $"XP decay is now **{(enabled.Value ? "enabled" : "disabled")}**! " +
                (enabled.Value ? "Inactive users will lose XP." : "")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task XpdecayRate(long xpPerDay, int inactiveDays)
        {
            if (xpPerDay < 1 || xpPerDay > 10000)
            {
                await Response().Error("XP per day must be between 1 and 10,000!").SendAsync();
                return;
            }

            if (inactiveDays < 1 || inactiveDays > 90)
            {
                await Response().Error("Inactive days threshold must be between 1 and 90!").SendAsync();
                return;
            }

            await _service.SetRateAsync(ctx.Guild.Id, xpPerDay, inactiveDays);
            await Response().Confirm(
                $"XP decay rate set: lose **{xpPerDay} XP/day** after **{inactiveDays} days** of inactivity.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task XpdecayInfo()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Confirm("XP decay is not configured.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F4C9 XP Decay Settings")
                .AddField("Enabled", config.Enabled.ToString(), true)
                .AddField("Inactive Threshold", $"{config.InactiveDays} days", true)
                .AddField("Decay Rate", $"{config.XpLostPerDay} XP/day", true)
                .WithFooter("XP decays daily at 3 AM UTC for inactive users. Floor: 0 XP.");

            await Response().Embed(eb).SendAsync();
        }
    }
}
