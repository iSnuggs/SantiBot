#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutomodCommands : SantiModule<AutomodService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodAdd(AutomodFilterType filterType, AutomodAction action = AutomodAction.Delete, int threshold = 5)
        {
            var rule = await _service.AddRuleAsync(ctx.Guild.Id, filterType, action, threshold);

            await Response().Confirm(strs.automod_rule_added(
                rule.Id,
                filterType.ToString(),
                action.ToString(),
                threshold)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodRemove(int ruleId)
        {
            var removed = await _service.RemoveRuleAsync(ctx.Guild.Id, ruleId);

            if (removed)
                await Response().Confirm(strs.automod_rule_removed(ruleId)).SendAsync();
            else
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodToggle(int ruleId)
        {
            var enabled = await _service.ToggleRuleAsync(ctx.Guild.Id, ruleId);

            await Response().Confirm(strs.automod_rule_toggled(ruleId, enabled ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodList()
        {
            var rules = await _service.GetAllRulesAsync(ctx.Guild.Id);

            if (rules.Count == 0)
            {
                await Response().Confirm(strs.automod_no_rules).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Automod Rules")
                .WithOkColor();

            foreach (var rule in rules)
            {
                var status = rule.Enabled ? "✅" : "❌";
                var exemptions = rule.Exemptions?.Count > 0
                    ? $" ({rule.Exemptions.Count} exemptions)"
                    : "";

                embed.AddField(
                    $"{status} Rule #{rule.Id} — {rule.FilterType}",
                    $"**Action:** {rule.Action}" +
                    (rule.ActionDurationMinutes > 0 ? $" ({rule.ActionDurationMinutes}m)" : "") +
                    $"\n**Threshold:** {rule.Threshold}" +
                    (rule.TimeWindowSeconds > 0 ? $" in {rule.TimeWindowSeconds}s" : "") +
                    (!string.IsNullOrEmpty(rule.PatternOrList) ? $"\n**Pattern:** `{Truncate(rule.PatternOrList, 50)}`" : "") +
                    exemptions,
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodThreshold(int ruleId, int threshold)
        {
            var rule = await _service.UpdateRuleAsync(ctx.Guild.Id, ruleId, threshold: threshold);

            if (rule is null)
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
            else
                await Response().Confirm(strs.automod_threshold_set(ruleId, threshold)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodSetAction(int ruleId, AutomodAction action, int durationMinutes = 0)
        {
            var rule = await _service.UpdateRuleAsync(ctx.Guild.Id, ruleId, action: action, actionDuration: durationMinutes);

            if (rule is null)
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
            else
                await Response().Confirm(strs.automod_action_set(ruleId, action.ToString())).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodPattern(int ruleId, [Leftover] string pattern)
        {
            var rule = await _service.UpdateRuleAsync(ctx.Guild.Id, ruleId, pattern: pattern);

            if (rule is null)
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
            else
                await Response().Confirm(strs.automod_pattern_set(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodExemptChannel(int ruleId, ITextChannel channel)
        {
            var success = await _service.AddExemptionAsync(ctx.Guild.Id, ruleId, AutomodExemptionType.Channel, channel.Id);

            if (success)
                await Response().Confirm(strs.automod_exempt_channel(ruleId, channel.Mention)).SendAsync();
            else
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodExemptRole(int ruleId, IRole role)
        {
            var success = await _service.AddExemptionAsync(ctx.Guild.Id, ruleId, AutomodExemptionType.Role, role.Id);

            if (success)
                await Response().Confirm(strs.automod_exempt_role(ruleId, role.Mention)).SendAsync();
            else
                await Response().Error(strs.automod_rule_not_found(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodInfractions(IGuildUser user)
        {
            var count = await _service.GetInfractionCountAsync(ctx.Guild.Id, user.Id);
            var recent = await _service.GetInfractionCountAsync(ctx.Guild.Id, user.Id, TimeSpan.FromHours(24));

            await Response().Confirm(strs.automod_infractions(
                user.Mention,
                count,
                recent)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task AutomodClearInfractions(IGuildUser user)
        {
            await _service.ClearInfractionsAsync(ctx.Guild.Id, user.Id);
            await Response().Confirm(strs.automod_infractions_cleared(user.Mention)).SendAsync();
        }

        private static string Truncate(string s, int maxLen)
            => s.Length > maxLen ? s[..maxLen] + "..." : s;
    }
}
