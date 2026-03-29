#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("lockpreset")]
    public partial class LockdownPresetsCommands : SantiModule<LockdownPresetsService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LockPresetSave([Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            {
                await Response().Error("Preset name must be 1-50 characters.").SendAsync();
                return;
            }

            var preset = await _service.SavePresetAsync(ctx.Guild.Id, name, ctx.Guild, ctx.User.Id);
            await Response().Confirm($"Lockdown preset **{name}** saved (ID: #{preset.Id}).").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LockPresetLoad([Leftover] string name)
        {
            var embed = CreateEmbed()
                .WithDescription($"⚠️ This will overwrite ALL channel permissions with the **{name}** preset.\nType `yes` to confirm.")
                .WithErrorColor();

            await Response().Embed(embed).SendAsync();

            var input = await GetUserInputAsync(ctx.User.Id, ctx.Channel.Id);
            if (input?.ToUpperInvariant() != "YES")
            {
                await Response().Error("Preset load cancelled.").SendAsync();
                return;
            }

            if (await _service.LoadPresetAsync(ctx.Guild.Id, name, ctx.Guild))
                await Response().Confirm($"Lockdown preset **{name}** loaded successfully.").SendAsync();
            else
                await Response().Error($"Preset **{name}** not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LockPresetList()
        {
            var presets = await _service.ListPresetsAsync(ctx.Guild.Id);
            if (presets.Count == 0)
            {
                await Response().Error("No lockdown presets saved.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Lockdown Presets")
                .WithDescription(string.Join("\n", presets.Select(p => $"• **{p.Name}** (saved {p.DateAdded:g})")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task LockPresetDel([Leftover] string name)
        {
            if (await _service.DeletePresetAsync(ctx.Guild.Id, name))
                await Response().Confirm($"Lockdown preset **{name}** deleted.").SendAsync();
            else
                await Response().Error($"Preset **{name}** not found.").SendAsync();
        }
    }
}
