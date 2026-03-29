#nullable disable
using SantiBot.Modules.Games.Dungeon;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Raid Boss")]
    [Group("raidboss")]
    public partial class RaidBossCommands : SantiModule<RaidBossService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Spawn([Leftover] string bossName = null)
        {
            var (success, message) = await _service.SpawnBossAsync(
                ctx.Guild.Id, ctx.Channel.Id, bossName);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Attack()
        {
            var (success, message) = await _service.AttackBossAsync(
                ctx.Guild.Id, ctx.User.Id, ctx.User.Username);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Status()
        {
            var (success, message, _) = await _service.GetStatusAsync(ctx.Guild.Id);
            if (success)
            {
                var eb = CreateEmbed()
                    .WithDescription(message)
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
            }
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Leaderboard()
        {
            var (success, message) = await _service.GetLeaderboardAsync(ctx.Guild.Id);
            if (success)
            {
                var eb = CreateEmbed()
                    .WithDescription(message)
                    .WithOkColor();
                await Response().Embed(eb).SendAsync();
            }
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Config(ITextChannel channel, int minClears = 5, int maxClears = 1000)
        {
            var (success, message) = await _service.ConfigureAsync(
                ctx.Guild.Id, channel.Id, minClears, maxClears, true);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task Toggle()
        {
            // Quick toggle for random spawns
            var (success, message) = await _service.ConfigureAsync(
                ctx.Guild.Id, ctx.Channel.Id, 5, 1000, true);
            if (success)
                await Response().Confirm("Random raid boss spawns toggled! Use `.raidboss config #channel min max` for fine-tuning.").SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bosses()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Available Raid Bosses\n");
            foreach (var (name, emoji, hp, atk, def, xp, currency, desc) in RaidBossService.BossTemplates)
            {
                sb.AppendLine($"{emoji} **{name}** — HP: {hp:N0} | ATK: {atk} | DEF: {def}");
                sb.AppendLine($"  *{desc}*");
                sb.AppendLine($"  Rewards: {xp} XP + {currency} currency\n");
            }
            sb.AppendLine("Admins: `.raidboss spawn <name>` or `.raidboss spawn` for random!");

            var eb = CreateEmbed()
                .WithDescription(sb.ToString())
                .WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
