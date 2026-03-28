#nullable disable
using SantiBot.Modules.Games.Heist;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Group("heist")]
    [Name("Heist")]
    public partial class HeistCommands(HeistService hs, ICurrencyProvider cp) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Heist(long amount)
        {
            if (amount < 100)
            {
                await Response().Error(strs.heist_min_bet).SendAsync();
                return;
            }

            var session = await hs.StartHeistAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id, amount);
            if (session is null)
            {
                await Response().Error(strs.heist_already_active).SendAsync();
                return;
            }

            var sign = cp.GetCurrencySign();
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Heist Recruiting!")
                .WithDescription(
                    $"{ctx.User.Mention} is planning a heist!\n\n"
                    + $"Pot: **{amount}**{sign}\n"
                    + $"Type `.heist join` within 60 seconds to join the crew!")
                .WithFooter("The heist begins in 60 seconds...");

            await Response().Embed(eb).SendAsync();

            // Wait 60 seconds then execute
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
                var (success, narrative, payout, winners) = await hs.ExecuteHeistAsync(ctx.Guild.Id);

                var resultEmbed = CreateEmbed();
                if (success)
                {
                    var share = payout / Math.Max(winners.Count, 1);
                    var winnerMentions = string.Join(", ", winners.Select(w => $"<@{w}>"));
                    resultEmbed
                        .WithOkColor()
                        .WithTitle("Heist Successful!")
                        .WithDescription(
                            $"{narrative}\n\n"
                            + $"Total Payout: **{payout}**{sign}\n"
                            + $"Each crew member receives: **{share}**{sign}\n"
                            + $"Winners: {winnerMentions}");
                }
                else
                {
                    resultEmbed
                        .WithErrorColor()
                        .WithTitle("Heist Failed!")
                        .WithDescription(
                            $"{narrative}\n\n"
                            + "The crew lost their bets. Better luck next time!");
                }

                await Response().Embed(resultEmbed).SendAsync();
            });
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task HeistJoin()
        {
            var (session, error) = await hs.JoinHeistAsync(ctx.Guild.Id, ctx.User.Id);

            if (session is null)
            {
                var errMsg = error switch
                {
                    "no_heist" => strs.heist_none_active,
                    "already_joined" => strs.heist_already_joined,
                    "not_enough" => strs.heist_not_enough,
                    _ => strs.heist_none_active,
                };
                await Response().Error(errMsg).SendAsync();
                return;
            }

            var crewSize = session.ParticipantIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            var sign = cp.GetCurrencySign();

            await Response().Confirm(strs.heist_joined(
                ctx.User.Mention,
                crewSize.ToString(),
                session.PotAmount.ToString() + sign)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Heist()
        {
            var session = await hs.GetActiveHeistAsync(ctx.Guild.Id);
            if (session is null)
            {
                await Response().Error(strs.heist_none_active).SendAsync();
                return;
            }

            var ids = session.ParticipantIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var sign = cp.GetCurrencySign();
            var elapsed = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Active Heist")
                .AddField("Status", session.Status, true)
                .AddField("Pot", $"{session.PotAmount}{sign}", true)
                .AddField("Crew Size", ids.Length.ToString(), true)
                .AddField("Time Elapsed", $"{elapsed}s", true);

            await Response().Embed(eb).SendAsync();
        }
    }
}
