#nullable disable
using SantiBot.Modules.Games.TowerDefense;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("TowerDefense")]
    [Group("td")]
    public partial class TowerDefenseCommands : SantiModule<TowerDefenseService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            var (success, message) = _service.StartGame(ctx.Channel.Id, ctx.User.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Place(string towerType, int position)
        {
            var (success, message) = _service.PlaceTower(ctx.Channel.Id, towerType, position);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Upgrade(int position)
        {
            var (success, message) = _service.UpgradeTower(ctx.Channel.Id, position);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Wave()
        {
            var (success, message) = await _service.RunWaveAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var status = _service.GetStatus(ctx.Channel.Id);
            if (status is null)
            {
                await Response().Error("No active Tower Defense game!").SendAsync();
                return;
            }

            await Response().Confirm($"🏰 **Tower Defense Status**\n```\n{status}\n```").SendAsync();
        }
    }
}
