#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("LevelColor")]
    [Group("levelcolor")]
    public partial class LevelColorCommands : SantiModule<LevelColorService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LevelcolorToggle(string toggle)
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
            await Response().Confirm($"Level-based role color is now **{(enabled.Value ? "enabled" : "disabled")}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LevelcolorSet(string startHex, string endHex)
        {
            if (!IsValidHex(startHex) || !IsValidHex(endHex))
            {
                await Response().Error("Please provide valid hex colors! (e.g., #3498DB)").SendAsync();
                return;
            }

            await _service.SetColorsAsync(ctx.Guild.Id, startHex, endHex);

            var startColor = LevelColorService.InterpolateColor(startHex, endHex, 1);
            var midColor = LevelColorService.InterpolateColor(startHex, endHex, 50);
            var endColor = LevelColorService.InterpolateColor(startHex, endHex, 100);

            var eb = CreateEmbed()
                .WithTitle("\U0001F308 Level Color Gradient Set!")
                .WithDescription(
                    $"**Start (L1):** `{startHex}`\n" +
                    $"**Middle (L50):** interpolated\n" +
                    $"**End (L100):** `{endHex}`\n\n" +
                    $"Role colors will gradually shift as users level up!")
                .WithColor(midColor);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task LevelcolorInfo()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Confirm("Level color is not configured.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F308 Level Color Config")
                .AddField("Enabled", config.Enabled.ToString(), true)
                .AddField("Start Color", config.StartColor, true)
                .AddField("End Color", config.EndColor, true);

            await Response().Embed(eb).SendAsync();
        }

        private static bool IsValidHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return false;
            return hex[1..].All(c => "0123456789ABCDEFabcdef".Contains(c));
        }
    }
}
