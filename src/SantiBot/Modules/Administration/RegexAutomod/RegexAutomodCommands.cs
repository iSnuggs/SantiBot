#nullable disable
using SantiBot.Modules.Administration.Services;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group("regex")]
    public partial class RegexAutomodCommands : SantiModule<RegexAutomodService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task RegexAdd(string pattern, string action = "delete")
        {
            if (!Enum.TryParse<RegexAutomodAction>(action, true, out var parsedAction))
            {
                await Response().Error("Invalid action. Use: delete, warn, mute, ban").SendAsync();
                return;
            }

            var rule = await _service.AddRuleAsync(ctx.Guild.Id, pattern, parsedAction, ctx.User.Id);
            if (rule is null)
            {
                await Response().Error("Invalid regex pattern.").SendAsync();
                return;
            }

            await Response().Confirm($"Regex automod rule **#{rule.Id}** added.\nPattern: `{pattern}`\nAction: **{parsedAction}**").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task RegexDel(int id)
        {
            var removed = await _service.RemoveRuleAsync(ctx.Guild.Id, id);
            if (!removed)
            {
                await Response().Error("Rule not found.").SendAsync();
                return;
            }

            await Response().Confirm($"Regex automod rule **#{id}** deleted.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task RegexList()
        {
            var rules = await _service.ListRulesAsync(ctx.Guild.Id);
            if (rules.Count == 0)
            {
                await Response().Error("No regex automod rules configured.").SendAsync();
                return;
            }

            var embed = CreateEmbed()
                .WithTitle("Regex Automod Rules")
                .WithOkColor();

            foreach (var r in rules)
            {
                embed.AddField($"#{r.Id} [{r.Action}] {(r.IsEnabled ? "✅" : "❌")}",
                    $"`{r.Pattern}`");
            }

            await Response().Embed(embed).SendAsync();
        }
    }
}
