#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class TicketCommands : SantiModule<TicketService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketEnable()
        {
            var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);
            var newState = !config.Enabled;
            await _service.EnableAsync(ctx.Guild.Id, newState);

            await Response().Confirm(strs.ticket_toggled(newState ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task TicketCreate([Leftover] string topic = null)
        {
            var result = await _service.CreateTicketAsync(ctx.Guild.Id, ctx.User.Id, topic);

            if (result.success)
                await Response().Confirm(strs.ticket_created(result.channelId)).SendAsync();
            else
                await Response().Error(strs.ticket_create_failed(result.error)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TicketClose()
        {
            var success = await _service.CloseTicketByChannelAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id);

            if (!success)
                await Response().Error(strs.ticket_not_ticket_channel).SendAsync();
            // If success, channel will be deleted — no need to respond
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TicketClaim()
        {
            var success = await _service.ClaimTicketByChannelAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id);

            if (success)
                await Response().Confirm(strs.ticket_claimed(ctx.User.Mention)).SendAsync();
            else
                await Response().Error(strs.ticket_claim_failed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task TicketList()
        {
            var tickets = await _service.GetOpenTicketsAsync(ctx.Guild.Id);

            if (tickets.Count == 0)
            {
                await Response().Confirm(strs.ticket_none_open).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Open Tickets")
                .WithOkColor();

            foreach (var t in tickets.Take(20))
            {
                var status = t.Status == TicketStatus.Claimed ? "🙋 Claimed" : "🎫 Open";
                embed.AddField(
                    $"#{t.TicketNumber} — {status}",
                    $"By: <@{t.CreatorUserId}>" +
                    (t.ClaimedByUserId.HasValue ? $" | Claimed: <@{t.ClaimedByUserId}>" : "") +
                    $"\nChannel: <#{t.ChannelId}>" +
                    (!string.IsNullOrEmpty(t.Topic) ? $"\nTopic: {t.Topic}" : ""),
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        // ── Configuration Commands ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketCategory(ICategoryChannel category = null)
        {
            await _service.SetCategoryAsync(ctx.Guild.Id, category?.Id);

            if (category is not null)
                await Response().Confirm(strs.ticket_category_set(category.Name)).SendAsync();
            else
                await Response().Confirm(strs.ticket_category_cleared).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketLog(ITextChannel channel = null)
        {
            await _service.SetLogChannelAsync(ctx.Guild.Id, channel?.Id);

            if (channel is not null)
                await Response().Confirm(strs.ticket_log_set(channel.Mention)).SendAsync();
            else
                await Response().Confirm(strs.ticket_log_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketRole(IRole role = null)
        {
            await _service.SetSupportRoleAsync(ctx.Guild.Id, role?.Id);

            if (role is not null)
                await Response().Confirm(strs.ticket_role_set(role.Mention)).SendAsync();
            else
                await Response().Confirm(strs.ticket_role_cleared).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketWelcome([Leftover] string message)
        {
            await _service.SetWelcomeMessageAsync(ctx.Guild.Id, message);
            await Response().Confirm(strs.ticket_welcome_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketMax(int maxPerUser)
        {
            if (maxPerUser < 0) maxPerUser = 0;
            await _service.SetMaxTicketsAsync(ctx.Guild.Id, maxPerUser);
            await Response().Confirm(strs.ticket_max_set(maxPerUser == 0 ? "unlimited" : maxPerUser.ToString())).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task TicketPanel()
        {
            await _service.SendPanelAsync(ctx.Guild.Id, (ITextChannel)ctx.Channel);
            // Panel is sent as its own message, no additional response needed
        }
    }
}
