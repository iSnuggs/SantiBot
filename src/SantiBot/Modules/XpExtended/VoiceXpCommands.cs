#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("VoiceXp")]
    [Group("voicexp")]
    public partial class VoiceXpCommands : SantiModule<VoiceXpService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VoicexpToggle(string toggle)
        {
            var enabled = toggle.ToLowerInvariant() switch
            {
                "on" or "enable" or "true" => true,
                "off" or "disable" or "false" => false,
                _ => (bool?)null
            };

            if (enabled is null)
            {
                await Response().Error("Use `on` or `off`.").SendAsync();
                return;
            }

            await _service.SetEnabledAsync(ctx.Guild.Id, enabled.Value);
            await Response().Confirm($"Voice XP is now **{(enabled.Value ? "enabled" : "disabled")}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task VoicexpRate(int xpPerMinute)
        {
            if (xpPerMinute < 1 || xpPerMinute > 100)
            {
                await Response().Error("XP per minute must be between 1 and 100!").SendAsync();
                return;
            }

            await _service.SetRateAsync(ctx.Guild.Id, xpPerMinute);
            await Response().Confirm($"Voice XP rate set to **{xpPerMinute} XP/minute**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task VoicexpInfo()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            var eb = CreateEmbed()
                .WithTitle("\U0001F3A4 Voice XP Settings")
                .AddField("Enabled", config?.Enabled.ToString() ?? "Not configured", true)
                .AddField("XP/Minute", config?.XpPerMinute.ToString() ?? "5 (default)", true)
                .WithFooter("Muted+deafened users don't earn Voice XP");

            await Response().Embed(eb).SendAsync();
        }
    }
}
