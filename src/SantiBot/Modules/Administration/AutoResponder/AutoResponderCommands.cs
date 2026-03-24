#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutoResponderCommands : SantiModule<AutoResponderService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseAdd(AutoResponseTriggerType triggerType, string trigger, [Leftover] string response)
        {
            if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(response))
            {
                await Response().Error(strs.autoresponse_missing_args).SendAsync();
                return;
            }

            var ar = await _service.AddResponseAsync(ctx.Guild.Id, trigger, triggerType, response);

            await Response().Confirm(strs.autoresponse_added(
                ar.Id,
                triggerType.ToString(),
                trigger)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseRemove(int responseId)
        {
            var removed = await _service.RemoveResponseAsync(ctx.Guild.Id, responseId);

            if (removed)
                await Response().Confirm(strs.autoresponse_removed(responseId)).SendAsync();
            else
                await Response().Error(strs.autoresponse_not_found(responseId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseToggle(int responseId)
        {
            var enabled = await _service.ToggleResponseAsync(ctx.Guild.Id, responseId);

            await Response().Confirm(strs.autoresponse_toggled(
                responseId, enabled ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseList()
        {
            var responses = await _service.GetAllResponsesAsync(ctx.Guild.Id);

            if (responses.Count == 0)
            {
                await Response().Confirm(strs.autoresponse_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Auto-Responses")
                .WithOkColor();

            foreach (var ar in responses)
            {
                var status = ar.Enabled ? "✅" : "❌";
                var extras = new List<string>();
                if (ar.DeleteTrigger) extras.Add("deletes trigger");
                if (ar.UserCooldownSeconds > 0) extras.Add($"{ar.UserCooldownSeconds}s user CD");
                if (ar.ChannelCooldownSeconds > 0) extras.Add($"{ar.ChannelCooldownSeconds}s channel CD");

                embed.AddField(
                    $"{status} #{ar.Id} — {ar.TriggerType}: `{Truncate(ar.Trigger, 30)}`",
                    $"**Response:** {Truncate(ar.ResponseText, 80)}" +
                    (extras.Count > 0 ? $"\n{string.Join(" | ", extras)}" : ""),
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseCooldown(int responseId, int userCooldownSeconds, int channelCooldownSeconds = 0)
        {
            var updated = await _service.UpdateResponseAsync(ctx.Guild.Id, responseId,
                userCooldown: userCooldownSeconds, channelCooldown: channelCooldownSeconds);

            if (updated is null)
                await Response().Error(strs.autoresponse_not_found(responseId)).SendAsync();
            else
                await Response().Confirm(strs.autoresponse_cooldown_set(responseId, userCooldownSeconds, channelCooldownSeconds)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutoResponseDeleteTrigger(int responseId)
        {
            var current = (await _service.GetAllResponsesAsync(ctx.Guild.Id))
                .FirstOrDefault(r => r.Id == responseId);

            if (current is null)
            {
                await Response().Error(strs.autoresponse_not_found(responseId)).SendAsync();
                return;
            }

            var newValue = !current.DeleteTrigger;
            await _service.UpdateResponseAsync(ctx.Guild.Id, responseId, deleteTrigger: newValue);

            await Response().Confirm(strs.autoresponse_delete_trigger(responseId, newValue ? "enabled" : "disabled")).SendAsync();
        }

        private static string Truncate(string s, int maxLen)
            => s.Length > maxLen ? s[..maxLen] + "..." : s;
    }
}
