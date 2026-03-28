using SantiBot.Modules.Gambling.Common;
using SantiBot.Modules.Gambling.DailyStreak;
using SantiBot.Modules.Gambling.Services;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("DailyStreak")]
    [Group("daily")]
    public partial class DailyStreakCommands : GamblingModule<DailyStreakService>
    {
        private readonly DailyStreakService _streaks;
        private readonly DiscordSocketClient _client;

        public DailyStreakCommands(GamblingConfigService gcs,
            DailyStreakService streaks,
            DiscordSocketClient client) : base(gcs)
        {
            _streaks = streaks;
            _client = client;
        }

        [Cmd]
        public async Task Daily()
        {
            var (current, longest, reward, alreadyClaimed) = await _streaks.ClaimDailyAsync(ctx.User.Id);

            if (alreadyClaimed)
            {
                await Response().Error(strs.daily_already_claimed).SendAsync();
                return;
            }

            var multiplier = DailyStreakService.GetMultiplier(current);
            var eb = CreateEmbed()
                .WithOkColor()
                .WithDescription(GetText(strs.daily_claimed(N(reward), current, $"{multiplier:0.#}x")));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [Priority(1)]
        public async Task Streak()
        {
            var (current, longest, lastClaim) = await _streaks.GetStreakAsync(ctx.User.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.streak_title))
                .AddField(GetText(strs.streak_current), current.ToString(), true)
                .AddField(GetText(strs.streak_longest), longest.ToString(), true)
                .AddField(GetText(strs.streak_multiplier), $"{DailyStreakService.GetMultiplier(current):0.#}x", true);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [Priority(0)]
        public async Task Streak([Leftover] IUser user)
        {
            var (current, longest, lastClaim) = await _streaks.GetStreakAsync(user.Id);

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle(GetText(strs.streak_title_user(user.ToString())))
                .AddField(GetText(strs.streak_current), current.ToString(), true)
                .AddField(GetText(strs.streak_longest), longest.ToString(), true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
