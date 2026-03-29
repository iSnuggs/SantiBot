#nullable disable
namespace SantiBot.Modules.XpExtended;

public partial class XpExtended
{
    [Name("Season")]
    [Group("season")]
    public partial class SeasonCommands : SantiModule<SeasonService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Season()
        {
            var season = await _service.GetActiveSeasonAsync(ctx.Guild.Id);

            if (season is null)
            {
                await Response().Confirm("No active season! Admins can start one with `.season start`.").SendAsync();
                return;
            }

            var progress = await _service.GetProgressAsync(ctx.Guild.Id, ctx.User.Id, season.SeasonNumber);
            var xp = progress?.SeasonXp ?? 0;
            var level = _service.GetSeasonLevel(xp);
            var remaining = season.EndDate - DateTime.UtcNow;

            var eb = CreateEmbed()
                .WithTitle($"\U0001F3C6 Season {season.SeasonNumber}")
                .AddField("Your Level", $"{level}/50", true)
                .AddField("Season XP", xp.ToString("N0"), true)
                .AddField("Time Left", $"{remaining.Days}d {remaining.Hours}h", true)
                .AddField("Next Level", $"{(level + 1) * 1000 - xp:N0} XP needed", true);

            // show progress bar
            var barLen = 20;
            var filled = (int)((double)level / 50 * barLen);
            var bar = new string('\u2588', filled) + new string('\u2591', barLen - filled);
            eb.AddField("Progress", $"`[{bar}]` {level * 2}%");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task SeasonStart()
        {
            var season = await _service.CreateSeasonAsync(ctx.Guild.Id);
            await Response().Confirm(
                $"\U0001F3C6 Season {season.SeasonNumber} started! Ends on {season.EndDate:MMM dd, yyyy}.").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SeasonRewards()
        {
            var rewards = _service.GetRewards();
            var season = await _service.GetActiveSeasonAsync(ctx.Guild.Id);
            var progress = season is not null
                ? await _service.GetProgressAsync(ctx.Guild.Id, ctx.User.Id, season.SeasonNumber)
                : null;
            var currentLevel = progress is not null ? _service.GetSeasonLevel(progress.SeasonXp) : 0;
            var claimedLevel = progress?.ClaimedLevel ?? 0;

            var sb = new System.Text.StringBuilder();
            foreach (var (level, reward) in rewards.OrderBy(x => x.Key))
            {
                var status = claimedLevel >= level ? "\u2705" : currentLevel >= level ? "\U0001F381" : "\U0001F512";
                sb.AppendLine($"{status} **Level {level}** — {reward.Cookies:N0} \U0001F960 + Title: \"{reward.Title}\"");
            }

            var eb = CreateEmbed()
                .WithTitle("\U0001F381 Season Rewards")
                .WithDescription(sb.ToString())
                .WithFooter("\u2705 = Claimed  \U0001F381 = Ready  \U0001F512 = Locked");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task SeasonClaim(int level)
        {
            var season = await _service.GetActiveSeasonAsync(ctx.Guild.Id);
            if (season is null)
            {
                await Response().Error("No active season!").SendAsync();
                return;
            }

            var (success, error, reward) = await _service.ClaimRewardAsync(ctx.Guild.Id, ctx.User.Id, season.SeasonNumber, level);
            if (!success)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"\U0001F381 Claimed season level {level} reward! You received {reward:N0} \U0001F960!").SendAsync();
        }
    }
}
