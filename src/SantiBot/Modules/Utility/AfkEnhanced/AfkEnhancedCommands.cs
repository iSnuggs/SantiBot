namespace SantiBot.Modules.Utility;

public partial class Utility
{
    [Group]
    public partial class AfkEnhancedCommands : SantiModule<AfkEnhancedService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Afke([Leftover] string? message = null)
        {
            var msg = message ?? "";

            await _service.SetAfkAsync(ctx.Guild.Id, ctx.User.Id, msg);

            if (string.IsNullOrWhiteSpace(msg))
                await Response().Confirm(strs.afke_set_no_message(ctx.User.Mention)).SendAsync();
            else
                await Response().Confirm(strs.afke_set(ctx.User.Mention, msg)).SendAsync();
        }
    }
}
