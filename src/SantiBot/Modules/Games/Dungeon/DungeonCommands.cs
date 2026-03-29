#nullable disable
using SantiBot.Modules.Games.Dungeon;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Dungeon")]
    [Group("dungeon")]
    public partial class DungeonCommands : SantiModule<DungeonService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Enter(int difficulty = 1)
        {
            var (success, message) = _service.EnterDungeon(ctx.Channel.Id, ctx.User.Id, ctx.User.Username, difficulty);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Invite(IUser user)
        {
            var (success, message) = _service.InviteToParty(ctx.Channel.Id, user.Id, user.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Explore()
        {
            var (success, message) = await _service.ExploreAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Attack()
        {
            var (success, message) = await _service.AttackAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Flee()
        {
            var (success, message) = _service.FleeAsync(ctx.Channel.Id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            if (!_service.ActiveDungeons.TryGetValue(ctx.Channel.Id, out var run))
            {
                await Response().Error("No active dungeon run!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🏰 Dungeon (Difficulty {run.Difficulty})")
                .AddField("Room", $"{run.CurrentRoom}/{run.MaxRooms}", true)
                .AddField("Loot", $"{run.TotalLoot} 🥠", true)
                .WithOkColor();

            foreach (var p in run.Party)
                eb.AddField(p.Username, $"HP: {p.Hp}/{p.MaxHp} | ATK: {p.Attack}", true);

            if (run.CurrentMonster is not null)
                eb.AddField("Monster", $"{run.CurrentMonster} (HP: {run.MonsterHp})", false);

            await Response().Embed(eb).SendAsync();
        }
    }
}
