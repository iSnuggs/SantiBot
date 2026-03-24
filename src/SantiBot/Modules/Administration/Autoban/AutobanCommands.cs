#nullable disable
using SantiBot.Common;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class AutobanCommands : SantiModule<AutobanService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanAge(int minHours, PunishmentAction action = PunishmentAction.Ban)
        {
            if (minHours < 1)
            {
                await Response().Error(strs.autoban_age_invalid).SendAsync();
                return;
            }

            var rule = await _service.AddRuleAsync(ctx.Guild.Id, AutobanRuleType.AccountAge,
                action, minAgeHours: minHours,
                reason: $"Account younger than {minHours} hours");

            await Response().Confirm(strs.autoban_age_added(rule.Id, minHours, action.ToString())).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanName([Leftover] string patterns)
        {
            if (string.IsNullOrWhiteSpace(patterns))
            {
                await Response().Error(strs.autoban_name_empty).SendAsync();
                return;
            }

            // Convert space-separated to newline-separated for storage
            var normalized = patterns.Replace(" ", "\n");
            var rule = await _service.AddRuleAsync(ctx.Guild.Id, AutobanRuleType.Username,
                usernamePatterns: normalized,
                reason: "Username matches banned pattern");

            await Response().Confirm(strs.autoban_name_added(rule.Id, patterns)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanNoAvatar(PunishmentAction action = PunishmentAction.Kick)
        {
            var rule = await _service.AddRuleAsync(ctx.Guild.Id, AutobanRuleType.NoAvatar,
                action, reason: "No avatar (default profile picture)");

            await Response().Confirm(strs.autoban_noavatar_added(rule.Id, action.ToString())).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanRemove(int ruleId)
        {
            var removed = await _service.RemoveRuleAsync(ctx.Guild.Id, ruleId);

            if (removed)
                await Response().Confirm(strs.autoban_removed(ruleId)).SendAsync();
            else
                await Response().Error(strs.autoban_not_found(ruleId)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanToggle(int ruleId)
        {
            var enabled = await _service.ToggleRuleAsync(ctx.Guild.Id, ruleId);
            await Response().Confirm(strs.autoban_toggled(ruleId, enabled ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.BanMembers)]
        public async Task AutobanList()
        {
            var rules = await _service.GetAllRulesAsync(ctx.Guild.Id);

            if (rules.Count == 0)
            {
                await Response().Confirm(strs.autoban_none).SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Autoban Rules")
                .WithOkColor();

            foreach (var rule in rules)
            {
                var status = rule.Enabled ? "✅" : "❌";
                var detail = rule.RuleType switch
                {
                    AutobanRuleType.AccountAge => $"Accounts younger than **{rule.MinAccountAgeHours}h**",
                    AutobanRuleType.Username => $"Names matching: `{rule.UsernamePatterns?.Replace("\n", ", ")}`",
                    AutobanRuleType.NoAvatar => "Users with default avatar",
                    _ => "Unknown",
                };

                embed.AddField(
                    $"{status} #{rule.Id} — {rule.RuleType}",
                    $"{detail}\n**Action:** {rule.Action}",
                    false);
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
