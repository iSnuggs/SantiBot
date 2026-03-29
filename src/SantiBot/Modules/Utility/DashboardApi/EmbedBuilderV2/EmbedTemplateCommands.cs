namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("EmbedBuilderV2")]
    [Group("embedtemplate")]
    public partial class EmbedTemplateCommands : SantiModule<DashboardApi.EmbedBuilderV2.EmbedTemplateService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedTemplateList()
        {
            var templates = await _service.ListTemplatesAsync(ctx.Guild.Id);
            var builtIn = _service.GetBuiltInTemplates();

            var desc = "**Built-in Templates:**\n";
            desc += string.Join(", ", builtIn.Keys.Select(k => $"`{k}`"));
            desc += "\n\n";

            if (templates.Count > 0)
            {
                desc += "**Custom Templates:**\n";
                desc += string.Join("\n", templates.Select(t =>
                    $"`{t.Name}` ({t.Category ?? "general"})"));
            }
            else
            {
                desc += "*No custom templates yet.*";
            }

            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Embed Templates").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedTemplatePreview(string name)
        {
            var builtIn = _service.GetBuiltInTemplates();
            string json;

            if (builtIn.TryGetValue(name.ToLower(), out var builtInJson))
            {
                json = builtInJson;
            }
            else
            {
                var template = await _service.GetTemplateAsync(ctx.Guild.Id, name);
                if (template is null)
                {
                    await Response().Error("Template not found.").SendAsync();
                    return;
                }
                json = template.EmbedJson;
            }

            var embed = _service.BuildFromJson(json, ctx.Guild, ctx.User);
            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task EmbedTemplateSend(string name, ITextChannel channel = null)
        {
            channel ??= (ITextChannel)ctx.Channel;
            var builtIn = _service.GetBuiltInTemplates();
            string json;

            if (builtIn.TryGetValue(name.ToLower(), out var builtInJson))
            {
                json = builtInJson;
            }
            else
            {
                var template = await _service.GetTemplateAsync(ctx.Guild.Id, name);
                if (template is null)
                {
                    await Response().Error("Template not found.").SendAsync();
                    return;
                }
                json = template.EmbedJson;
            }

            var embed = _service.BuildFromJson(json, ctx.Guild, ctx.User);
            await _sender.Response(channel).Embed(embed).SendAsync();
            await Response().Confirm($"Embed sent to {channel.Mention}").SendAsync();
        }
    }
}
