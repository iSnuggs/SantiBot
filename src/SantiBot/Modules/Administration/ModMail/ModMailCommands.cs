#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ModMailCommands : SantiModule<ModMailService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailEnable()
        {
            var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);
            var newState = !config.Enabled;
            await _service.EnableAsync(ctx.Guild.Id, newState);

            await Response().Confirm(strs.modmail_toggled(newState ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailCategory(ICategoryChannel category = null)
        {
            await _service.SetCategoryAsync(ctx.Guild.Id, category?.Id);

            if (category is not null)
                await Response().Confirm(strs.modmail_category_set(category.Name)).SendAsync();
            else
                await Response().Confirm(strs.modmail_category_cleared).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailLog(ITextChannel channel = null)
        {
            await _service.SetLogChannelAsync(ctx.Guild.Id, channel?.Id);

            if (channel is not null)
                await Response().Confirm(strs.modmail_log_set(channel.Mention)).SendAsync();
            else
                await Response().Confirm(strs.modmail_log_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailRole(IRole role = null)
        {
            await _service.SetStaffRoleAsync(ctx.Guild.Id, role?.Id);

            if (role is not null)
                await Response().Confirm(strs.modmail_role_set(role.Mention)).SendAsync();
            else
                await Response().Confirm(strs.modmail_role_cleared).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModMailClose()
        {
            var success = await _service.CloseThreadByChannelAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id);

            if (!success)
                await Response().Error(strs.modmail_not_thread).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModMailBlock(IUser user, [Leftover] string reason = null)
        {
            var success = await _service.BlockUserAsync(ctx.Guild.Id, user.Id, ctx.User.Id, reason);

            if (success)
                await Response().Confirm(strs.modmail_blocked(user.Mention)).SendAsync();
            else
                await Response().Error(strs.modmail_already_blocked(user.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModMailUnblock(IUser user)
        {
            var success = await _service.UnblockUserAsync(ctx.Guild.Id, user.Id);

            if (success)
                await Response().Confirm(strs.modmail_unblocked(user.Mention)).SendAsync();
            else
                await Response().Error(strs.modmail_not_blocked(user.Mention)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModMailThreads()
        {
            var threads = await _service.GetRecentThreadsAsync(ctx.Guild.Id, 15);

            if (threads.Count == 0)
            {
                await Response().Confirm(strs.modmail_no_threads).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Recent Mod Mail Threads")
                .WithOkColor();

            foreach (var t in threads)
            {
                var statusEmoji = t.Status == ModMailThreadStatus.Open ? "\ud83d\udce8" : "\ud83d\udd12";
                var duration = ((t.ClosedAt ?? DateTime.UtcNow) - t.CreatedAt).Humanize();
                embed.AddField(
                    $"{statusEmoji} <@{t.UserId}> — {t.CreatedAt:MMM dd, yyyy}",
                    $"Messages: {t.MessageCount} | Duration: {duration}" +
                    (t.Status == ModMailThreadStatus.Open ? $"\nChannel: <#{t.ChannelId}>" : ""),
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailOpenMsg([Leftover] string message)
        {
            await _service.SetOpenMessageAsync(ctx.Guild.Id, message);
            await Response().Confirm(strs.modmail_openmsg_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailCloseMsg([Leftover] string message)
        {
            await _service.SetCloseMessageAsync(ctx.Guild.Id, message);
            await Response().Confirm(strs.modmail_closemsg_set).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModMailConfig()
        {
            var config = await _service.GetOrCreateConfigAsync(ctx.Guild.Id);

            var embed = CreateEmbed()
                .WithTitle("Mod Mail Configuration")
                .AddField("Status", config.Enabled ? "Enabled" : "Disabled", true)
                .AddField("Category", config.CategoryId.HasValue ? $"<#{config.CategoryId}>" : "None", true)
                .AddField("Log Channel", config.LogChannelId.HasValue ? $"<#{config.LogChannelId}>" : "None", true)
                .AddField("Staff Role", config.StaffRoleId.HasValue ? $"<@&{config.StaffRoleId}>" : "None", true)
                .AddField("Open Message", config.OpenMessage ?? "(default)")
                .AddField("Close Message", config.CloseMessage ?? "(default)")
                .WithOkColor();

            await Response().Embed(embed).SendAsync();
        }
    }
}
