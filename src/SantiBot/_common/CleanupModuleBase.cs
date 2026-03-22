#nullable disable
namespace SantiBot.Common;

public abstract class CleanupModuleBase : SantiModule
{
    protected async Task ConfirmActionInternalAsync(string name, Func<Task> action)
    {
        try
        {
            var embed = CreateEmbed()
                .WithTitle(GetText(strs.sql_confirm_exec))
                .WithDescription(name);

            if (!await PromptUserConfirmAsync(embed))
                return;

            await action();
            await ctx.OkAsync();
        }
        catch (Exception ex)
        {
            await Response().Error(ex.ToString()).SendAsync();
        }
    }
}