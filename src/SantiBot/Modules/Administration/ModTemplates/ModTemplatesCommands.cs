#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("modtemplate")]
    public partial class ModTemplatesCommands : SantiModule<ModTemplatesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModTemplateAdd(string name, [Leftover] string reason)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            {
                await Response().Error("Template name must be 1-50 characters.").SendAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                await Response().Error("Reason cannot be empty.").SendAsync();
                return;
            }

            if (await _service.AddTemplateAsync(ctx.Guild.Id, name.ToLowerInvariant(), reason))
                await Response().Confirm($"Mod template **{name}** created.\nReason: {reason}").SendAsync();
            else
                await Response().Error($"Template **{name}** already exists.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModTemplateDel([Leftover] string name)
        {
            if (await _service.DeleteTemplateAsync(ctx.Guild.Id, name.ToLowerInvariant()))
                await Response().Confirm($"Mod template **{name}** deleted.").SendAsync();
            else
                await Response().Error($"Template **{name}** not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task ModTemplateList()
        {
            var templates = await _service.ListTemplatesAsync(ctx.Guild.Id);
            if (templates.Count == 0)
            {
                await Response().Error("No mod templates configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Mod Action Templates")
                .WithOkColor();

            foreach (var t in templates)
                embed.AddField($"**{t.Name}**", t.Reason);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task ModTemplateUse([Leftover] string name)
        {
            var template = await _service.GetTemplateAsync(ctx.Guild.Id, name.ToLowerInvariant());
            if (template is null)
            {
                await Response().Error($"Template **{name}** not found.").SendAsync();
                return;
            }

            await Response().Confirm($"**Template: {template.Name}**\n\n{template.Reason}\n\n*Copy this reason when using warn/ban/kick commands.*").SendAsync();
        }
    }
}
