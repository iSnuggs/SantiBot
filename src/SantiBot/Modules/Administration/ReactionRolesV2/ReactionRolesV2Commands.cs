#nullable disable
using SantiBot.Modules.Administration.Services;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("ddrole")]
    public partial class ReactionRolesV2Commands : SantiModule<ReactionRolesV2Service>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task DropdownRoleCreate([Leftover] string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await Response().Error("Please provide a title for the dropdown panel.").SendAsync();
                return;
            }

            var panel = await _service.CreatePanelAsync(ctx.Guild.Id, ctx.Channel.Id, title);

            var cb = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId(_service.GetCustomId())
                    .WithPlaceholder("Select your roles...")
                    .WithMinValues(0)
                    .WithMaxValues(1)
                    .AddOption("Placeholder", "placeholder", "Add roles with .ddrole add"));

            var embed = CreateEmbed()
                .WithTitle(title)
                .WithDescription("Use the dropdown below to select your roles.")
                .WithOkColor();

            var msg = await ctx.Channel.SendMessageAsync(embed: embed.Build(), components: cb.Build());
            await _service.UpdatePanelMessageIdAsync(panel.Id, msg.Id);

            await Response().Confirm($"Dropdown role panel created! Message ID: `{msg.Id}`\nAdd roles with `.ddrole add {msg.Id} <label> <role>`").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task DropdownRoleAdd(ulong messageId, string label, IRole role)
        {
            var option = await _service.AddOptionAsync(ctx.Guild.Id, messageId, label, role.Id);
            if (option is null)
            {
                await Response().Error("Panel not found for that message ID.").SendAsync();
                return;
            }

            // Rebuild the select menu with all options
            var allOptions = await _service.GetOptionsAsync(messageId);
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId(_service.GetCustomId())
                .WithPlaceholder("Select your roles...")
                .WithMinValues(0)
                .WithMaxValues(allOptions.Count);

            foreach (var opt in allOptions)
            {
                var guildRole = ctx.Guild.GetRole(opt.RoleId);
                menuBuilder.AddOption(opt.Label, opt.Label, guildRole?.Name ?? "Unknown Role");
            }

            var cb = new ComponentBuilder().WithSelectMenu(menuBuilder);

            try
            {
                var channel = ctx.Channel as ITextChannel;
                var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (msg is not null)
                    await msg.ModifyAsync(m => m.Components = cb.Build());
            }
            catch { }

            await Response().Confirm($"Added role option **{label}** → **{role.Name}** to the dropdown panel.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageRoles)]
        public async Task ButtonRole(ulong messageId, string emoji, IRole role)
        {
            // This uses the existing ButtonRole system but adds it via command
            var channel = ctx.Channel as ITextChannel;
            if (channel is null) return;

            try
            {
                var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
                if (msg is null)
                {
                    await Response().Error("Message not found in this channel.").SendAsync();
                    return;
                }

                var buttonId = $"n:btnrole:{role.Id}";
                var cb = ComponentBuilder.FromMessage(msg);

                // Add new button
                IEmote emote = Emote.TryParse(emoji, out var customEmote)
                    ? customEmote
                    : new Emoji(emoji);

                cb.WithButton(role.Name, buttonId, ButtonStyle.Primary, emote);

                await msg.ModifyAsync(m => m.Components = cb.Build());
                await Response().Confirm($"Button role added: {emoji} → **{role.Name}**").SendAsync();
            }
            catch (Exception ex)
            {
                await Response().Error($"Failed: {ex.Message}").SendAsync();
            }
        }
    }
}
