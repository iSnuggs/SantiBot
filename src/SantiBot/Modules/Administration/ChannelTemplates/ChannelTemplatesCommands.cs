#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("chtemplate")]
    public partial class ChannelTemplatesCommands : SantiModule<ChannelTemplatesService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task ChTemplateSave([Leftover] string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            {
                await Response().Error("Template name must be 1-50 characters.").SendAsync();
                return;
            }

            if (ctx.Channel is not ITextChannel textChannel)
            {
                await Response().Error("Must be used in a text channel.").SendAsync();
                return;
            }

            var template = await _service.SaveTemplateAsync(ctx.Guild.Id, name, textChannel, ctx.User.Id);
            await Response().Confirm($"Channel template **{name}** saved from #{ctx.Channel.Name}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task ChTemplateLoad(string name, [Leftover] string newName = null)
        {
            var channel = await _service.LoadTemplateAsync(ctx.Guild.Id, name, ctx.Guild, newName);
            if (channel is null)
            {
                await Response().Error($"Template **{name}** not found.").SendAsync();
                return;
            }

            await Response().Confirm($"Channel {channel.Mention} created from template **{name}**.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task ChTemplateList()
        {
            var templates = await _service.ListTemplatesAsync(ctx.Guild.Id);
            if (templates.Count == 0)
            {
                await Response().Error("No channel templates saved.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Channel Templates")
                .WithDescription(string.Join("\n", templates.Select(t => $"• **{t.Name}** (saved {t.DateAdded:g})")))
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageChannels)]
        public async Task ChTemplateDel([Leftover] string name)
        {
            if (await _service.DeleteTemplateAsync(ctx.Guild.Id, name))
                await Response().Confirm($"Channel template **{name}** deleted.").SendAsync();
            else
                await Response().Error($"Template **{name}** not found.").SendAsync();
        }
    }
}
