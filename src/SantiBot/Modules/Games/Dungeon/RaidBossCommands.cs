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
        public async Task Config(ITextChannel channel, int minClears = 5, int maxClears = 25)
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
                ctx.Guild.Id, ctx.Channel.Id, 5, 25, true);
            if (success)
                await Response().Confirm("Random raid boss spawns toggled! Use `.raidboss config #channel min max` for fine-tuning.").SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bosses([Leftover] string filter = null)
        {
            RaidBossService.BossEntry[] bosses;
            string title;

            // Filter by tier or search by name
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (System.Enum.TryParse<RaidBossService.BossTier>(filter, true, out var tier))
                {
                    bosses = RaidBossService.GetBossesByTier(tier);
                    title = $"{RaidBossService.TierEmoji(tier)} {tier} Raid Bosses ({bosses.Length})";
                }
                else
                {
                    bosses = RaidBossService.SearchBosses(filter);
                    title = $"Search: \"{filter}\" ({bosses.Length} results)";
                }
            }
            else
            {
                // Show tier summary by default
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# 1,000 Raid Bosses\n");
                foreach (RaidBossService.BossTier tier in System.Enum.GetValues<RaidBossService.BossTier>())
                {
                    var tierBosses = RaidBossService.GetBossesByTier(tier);
                    sb.AppendLine($"{RaidBossService.TierEmoji(tier)} **{tier}** — {tierBosses.Length} bosses");
                    // Show first 3 as preview
                    foreach (var b in tierBosses.Take(3))
                        sb.AppendLine($"  {b.Emoji} {b.Name} (HP: {b.BaseHp:N0})");
                    if (tierBosses.Length > 3)
                        sb.AppendLine($"  *...and {tierBosses.Length - 3} more*");
                    sb.AppendLine();
                }
                sb.AppendLine("**Browse:** `.raidboss bosses Common` / `Uncommon` / `Rare` / `Epic` / `Legendary` / `Mythic`");
                sb.AppendLine("**Search:** `.raidboss bosses dragon` — find by name");
                sb.AppendLine("**Spawn:** `.raidboss spawn <name>` or `.raidboss spawn` for random!");

                var overview = CreateEmbed().WithDescription(sb.ToString()).WithOkColor();
                await Response().Embed(overview).SendAsync();
                return;
            }

            if (bosses.Length == 0)
            {
                await Response().Error($"No bosses found matching \"{filter}\"!").SendAsync();
                return;
            }

            // Show up to 15 bosses per page
            var desc2 = new System.Text.StringBuilder();
            desc2.AppendLine($"# {title}\n");
            foreach (var b in bosses.Take(15))
            {
                desc2.AppendLine($"{RaidBossService.TierEmoji(b.Tier)} {b.Emoji} **{b.Name}** — HP: {b.BaseHp:N0} | ATK: {b.BaseAtk} | DEF: {b.BaseDef}");
                desc2.AppendLine($"  *{b.Desc}*");
                desc2.AppendLine($"  Rewards: {b.BaseXp} XP + {b.BaseCurrency} currency\n");
            }
            if (bosses.Length > 15)
                desc2.AppendLine($"*...and {bosses.Length - 15} more. Narrow your search to see them all.*");

            var eb = CreateEmbed().WithDescription(desc2.ToString()).WithOkColor();
            await Response().Embed(eb).SendAsync();
        }
    }
}
