#nullable disable
using SantiBot.Common;

namespace SantiBot.Modules.Administration;

public partial class Administration
{
    [Group]
    public partial class DehoistCommands : SantiModule<DehoistService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageNicknames)]
        public async Task Dehoist()
        {
            var config = await _service.GetConfigAsync(ctx.Guild.Id);
            var newState = config is null || !config.Enabled;
            await _service.EnableAsync(ctx.Guild.Id, newState);

            await Response().Confirm(strs.dehoist_toggled(newState ? "enabled" : "disabled")).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageNicknames)]
        public async Task DehoistAll()
        {
            var msg = await Response().Pending(strs.dehoist_scanning).SendAsync();
            var count = await _service.DehoistAllAsync(ctx.Guild.Id);
            await msg.DeleteAsync();
            await Response().Confirm(strs.dehoist_all_result(count)).SendAsync();
        }
    }
}
