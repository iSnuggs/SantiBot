#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("modtranslate")]
    public partial class ModTranslateCommands : SantiModule<ModTranslateService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModTranslate([Leftover] string targetLang)
        {
            if (targetLang.ToLowerInvariant() == "off")
            {
                await _service.SetConfigAsync(ctx.Guild.Id, "", false);
                await Response().Confirm("Mod translation disabled.").SendAsync();
                return;
            }

            await _service.SetConfigAsync(ctx.Guild.Id, targetLang.ToLowerInvariant(), true);
            await Response().Confirm($"Mod translation enabled. Target language: **{targetLang}**\nUse `.modtranslate off` to disable.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Translate([Leftover] string text)
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            if (config is null || !config.IsEnabled)
            {
                await Response().Error("Mod translation is not configured. Use `.modtranslate <lang>` first.").SendAsync();
                return;
            }

            var translated = await _service.TranslateAsync(text, config.TargetLanguage);
            if (translated is null)
            {
                await Response().Error("Translation failed.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Translation → {config.TargetLanguage.ToUpperInvariant()}")
                .AddField("Original", text.Length > 1024 ? text[..1021] + "..." : text)
                .AddField("Translated", translated.Length > 1024 ? translated[..1021] + "..." : translated)
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
