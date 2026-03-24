#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class ModCaseCommands : SantiModule<ModCaseService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Case(int caseNumber)
        {
            var modCase = await _service.GetCaseAsync(ctx.Guild.Id, caseNumber);

            if (modCase is null)
            {
                await Response().Error(strs.case_not_found(caseNumber)).SendAsync();
                return;
            }

            var guild = ctx.Guild as SocketGuild;
            var target = guild?.GetUser(modCase.TargetUserId);
            var mod = guild?.GetUser(modCase.ModeratorUserId);

            var embed = CreateEmbed()
                .WithTitle($"Case #{modCase.CaseNumber} — {modCase.CaseType}")
                .AddField("User", target?.ToString() ?? $"<@{modCase.TargetUserId}>", true)
                .AddField("Moderator", mod?.ToString() ?? $"<@{modCase.ModeratorUserId}>", true)
                .AddField("Reason", modCase.Reason)
                .WithTimestamp(modCase.CreatedAt)
                .WithOkColor();

            if (modCase.DurationMinutes > 0)
                embed.AddField("Duration", $"{modCase.DurationMinutes} minutes", true);

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Cases(IGuildUser user)
        {
            var cases = await _service.GetUserCasesAsync(ctx.Guild.Id, user.Id);

            if (cases.Count == 0)
            {
                await Response().Confirm(strs.cases_none(user.Mention)).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Mod Cases — {user}")
                .WithOkColor();

            foreach (var c in cases.Take(15))
            {
                embed.AddField(
                    $"#{c.CaseNumber} — {c.CaseType} ({c.CreatedAt:yyyy-MM-dd})",
                    Truncate(c.Reason, 80),
                    false);
            }

            if (cases.Count > 15)
                embed.WithFooter($"Showing 15 of {cases.Count} cases");

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task CaseLog()
        {
            var cases = await _service.GetRecentCasesAsync(ctx.Guild.Id);

            if (cases.Count == 0)
            {
                await Response().Confirm(strs.cases_log_empty).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Recent Mod Cases")
                .WithOkColor();

            foreach (var c in cases.Take(15))
            {
                embed.AddField(
                    $"#{c.CaseNumber} — {c.CaseType}",
                    $"<@{c.TargetUserId}> by <@{c.ModeratorUserId}>\n{Truncate(c.Reason, 60)}",
                    true);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task Reason(int caseNumber, [Leftover] string reason)
        {
            var success = await _service.UpdateReasonAsync(ctx.Guild.Id, caseNumber, reason);

            if (success)
                await Response().Confirm(strs.case_reason_updated(caseNumber)).SendAsync();
            else
                await Response().Error(strs.case_not_found(caseNumber)).SendAsync();
        }

        // ── Mod Notes ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModNote(IGuildUser user, [Leftover] string note)
        {
            var modNote = await _service.AddNoteAsync(ctx.Guild.Id, user.Id, ctx.User.Id, note);
            await Response().Confirm(strs.modnote_added(user.Mention, modNote.Id)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModNotes(IGuildUser user)
        {
            var notes = await _service.GetNotesAsync(ctx.Guild.Id, user.Id);

            if (notes.Count == 0)
            {
                await Response().Confirm(strs.modnotes_none(user.Mention)).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle($"Mod Notes — {user}")
                .WithOkColor();

            foreach (var n in notes.Take(15))
            {
                embed.AddField(
                    $"#{n.Id} by <@{n.ModeratorUserId}> ({n.CreatedAt:yyyy-MM-dd})",
                    Truncate(n.Content, 200),
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task ModNoteDelete(int noteId)
        {
            var success = await _service.DeleteNoteAsync(ctx.Guild.Id, noteId);

            if (success)
                await Response().Confirm(strs.modnote_deleted(noteId)).SendAsync();
            else
                await Response().Error(strs.modnote_not_found(noteId)).SendAsync();
        }

        // ── Mod Settings ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModLogChannel(ITextChannel channel = null)
        {
            await _service.SetModLogChannelAsync(ctx.Guild.Id, channel?.Id);

            if (channel is not null)
                await Response().Confirm(strs.modlog_channel_set(channel.Mention)).SendAsync();
            else
                await Response().Confirm(strs.modlog_channel_disabled).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModDm()
        {
            var settings = await _service.GetSettingsAsync(ctx.Guild.Id);
            var newValue = !settings.DmOnAction;
            await _service.SetDmOnActionAsync(ctx.Guild.Id, newValue);

            await Response().Confirm(strs.moddm_toggled(newValue ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task ModDmTemplate([Leftover] string template)
        {
            await _service.SetDmTemplateAsync(ctx.Guild.Id, template);
            await Response().Confirm(strs.moddm_template_set).SendAsync();
        }

        // ── Auto-Punish ──

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoPunish(int caseCount, PunishmentAction action, int timeWindowHours = 0, int durationMinutes = 0)
        {
            var config = await _service.AddAutoPunishAsync(ctx.Guild.Id, caseCount, action, timeWindowHours, durationMinutes);

            var windowText = timeWindowHours > 0 ? $" within {timeWindowHours} hours" : "";
            await Response().Confirm(strs.autopunish_added(caseCount, windowText, action.ToString())).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoPunishList()
        {
            var configs = await _service.GetAutoPunishConfigsAsync(ctx.Guild.Id);

            if (configs.Count == 0)
            {
                await Response().Confirm(strs.autopunish_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Auto-Punish Rules")
                .WithOkColor();

            foreach (var c in configs)
            {
                var window = c.TimeWindowHours > 0 ? $" in {c.TimeWindowHours}h" : " (all time)";
                var duration = c.ActionDurationMinutes > 0 ? $" for {c.ActionDurationMinutes}m" : "";
                embed.AddField(
                    $"#{c.Id} — {c.CaseCount} cases{window}",
                    $"Action: **{c.Action}**{duration}",
                    true);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoPunishRemove(int configId)
        {
            var success = await _service.RemoveAutoPunishAsync(ctx.Guild.Id, configId);

            if (success)
                await Response().Confirm(strs.autopunish_removed(configId)).SendAsync();
            else
                await Response().Error(strs.autopunish_not_found(configId)).SendAsync();
        }

        private static string Truncate(string s, int maxLen)
            => s?.Length > maxLen ? s[..maxLen] + "..." : s ?? "";
    }
}
