namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Name("CustomScripting")]
    [Group("customscript")]
    public partial class CustomScriptCommands : SantiModule<CustomScripting.CustomScriptService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CustomScriptAdd(string trigger, [Leftover] string script)
        {
            var success = await _service.AddScriptAsync(ctx.Guild.Id, trigger, script);
            if (success)
            {
                await Response()
                    .Confirm($"Custom script added for trigger `{trigger}`\n" +
                             "**Available placeholders:**\n" +
                             "`{user}` `{channel}` `{server}` `{args}`\n" +
                             "`{random:1-100}` `{pick:a|b|c}` `{if:cond:then:else}`\n" +
                             "`{time}` `{date}` `{user.name}` `{user.id}`")
                    .SendAsync();
            }
            else
            {
                await Response().Error("A script with that trigger already exists.").SendAsync();
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task CustomScriptRemove([Leftover] string trigger)
        {
            var success = await _service.RemoveScriptAsync(ctx.Guild.Id, trigger);
            if (success)
                await Response().Confirm($"Custom script `{trigger}` removed.").SendAsync();
            else
                await Response().Error("Script not found.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task CustomScriptList()
        {
            var scripts = await _service.ListScriptsAsync(ctx.Guild.Id);
            if (scripts.Count == 0)
            {
                await Response().Error("No custom scripts configured.").SendAsync();
                return;
            }

            var desc = string.Join("\n", scripts.Select((s, i) =>
                $"`{i + 1}.` **{s.Trigger}** -> {s.Script.TrimTo(60)} [{(s.IsEnabled ? "ON" : "OFF")}]"));
            await Response()
                .Embed(CreateEmbed().WithOkColor().WithTitle("Custom Scripts").WithDescription(desc))
                .SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CustomScriptTest(string trigger, [Leftover] string args = "")
        {
            var scripts = await _service.ListScriptsAsync(ctx.Guild.Id);
            var script = scripts.FirstOrDefault(s =>
                s.Trigger.Equals(trigger, System.StringComparison.OrdinalIgnoreCase));

            if (script is null)
            {
                await Response().Error("Script not found.").SendAsync();
                return;
            }

            var result = CustomScripting.CustomScriptService.ExecuteScript(
                script.Script, ctx.User, (ITextChannel)ctx.Channel, args);
            await Response().Confirm($"**Output:**\n{result}").SendAsync();
        }
    }
}
