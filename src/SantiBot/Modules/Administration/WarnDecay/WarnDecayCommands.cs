namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("warndecay")]
    [Name("WarnDecay")]
    public partial class WarnDecayCommands : SantiModule<WarnDecayService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WarnDecay()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);

            if (config is null)
            {
                await Response().Error(strs.warndecay_not_configured).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Warn Decay Configuration")
                .AddField("Status", config.Enabled ? "Enabled" : "Disabled", true)
                .AddField("Decay After", $"{config.DecayDays} days", true)
                .AddField("Min Warns to Decay", config.MinWarnsToDecay.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WarnDecayEnable()
        {
            await _service.EnableAsync(ctx.Guild.Id);
            await Response().Confirm(strs.warndecay_enabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WarnDecayDisable()
        {
            if (await _service.DisableAsync(ctx.Guild.Id))
                await Response().Confirm(strs.warndecay_disabled).SendAsync();
            else
                await Response().Error(strs.warndecay_not_configured).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task WarnDecayDays(int days)
        {
            if (days < 1 || days > 365)
            {
                await Response().Error(strs.warndecay_days_invalid).SendAsync();
                return;
            }

            if (await _service.SetDecayDaysAsync(ctx.Guild.Id, days))
                await Response().Confirm(strs.warndecay_days_set(days)).SendAsync();
            else
                await Response().Error(strs.warndecay_not_configured).SendAsync();
        }
    }
}
