#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutoDeleteCommands : SantiModule<AutoDeleteService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoDeleteAdd(ITextChannel channel, int delaySeconds = 5, [Leftover] string filter = null)
        {
            if (delaySeconds < 0) delaySeconds = 0;

            var rule = await _service.AddRuleAsync(ctx.Guild.Id, channel.Id, delaySeconds, filter);

            var filterDesc = string.IsNullOrEmpty(filter) ? "all messages" : $"filter: `{filter}`";
            await Response().Confirm(strs.autodelete_added(rule.Id, channel.Mention, delaySeconds, filterDesc)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoDeleteRemove(int ruleId)
        {
            var removed = await _service.RemoveRuleAsync(ctx.Guild.Id, ruleId);

            if (removed)
                await Response().Confirm(strs.autodelete_removed(ruleId)).SendAsync();
            else
                await Response().Error(strs.autodelete_not_found(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoDeleteToggle(int ruleId)
        {
            var enabled = await _service.ToggleRuleAsync(ctx.Guild.Id, ruleId);
            await Response().Confirm(strs.autodelete_toggled(ruleId, enabled ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task AutoDeleteList()
        {
            var rules = await _service.GetRulesAsync(ctx.Guild.Id);

            if (rules.Count == 0)
            {
                await Response().Confirm(strs.autodelete_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Auto-Delete Rules")
                .WithOkColor();

            foreach (var rule in rules)
            {
                var status = rule.Enabled ? "✅" : "❌";
                var filterText = rule.UseFilter && !string.IsNullOrEmpty(rule.Filter)
                    ? $"Filter: `{rule.Filter}`"
                    : "All messages";

                embed.AddField(
                    $"{status} #{rule.Id} — <#{rule.ChannelId}>",
                    $"**Delay:** {rule.DelaySeconds}s | {filterText}" +
                    (rule.IgnorePinned ? " | Pinned safe" : ""),
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
