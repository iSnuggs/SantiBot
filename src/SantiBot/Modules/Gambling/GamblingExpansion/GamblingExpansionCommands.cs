#nullable disable
using SantiBot.Modules.Gambling.GamblingExpansion;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("GamblingExpansion")]
    [Group("gamble")]
    public partial class GamblingExpansionCommands : SantiModule<GamblingExpansionService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Coinflip(long bet, [Leftover] string guess)
        {
            if (bet < 1)
            {
                await Response().Error("Minimum bet is 1 🥠!").SendAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(guess))
            {
                await Response().Error("Specify heads or tails!").SendAsync();
                return;
            }

            var (won, result, payout) = await _service.CoinflipAsync(ctx.User.Id, bet, guess.Trim());
            if (won)
                await Response().Confirm($"🪙 The coin landed on **{result}**! You won **{payout}** 🥠!").SendAsync();
            else if (payout == 0 && result != "Not enough 🥠!")
                await Response().Error($"🪙 The coin landed on **{result}**! You lost {bet} 🥠.").SendAsync();
            else
                await Response().Error(result).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Roulette(long bet, [Leftover] string guess)
        {
            if (bet < 1)
            {
                await Response().Error("Minimum bet is 1 🥠!").SendAsync();
                return;
            }

            var (won, result, payout) = await _service.RouletteAsync(ctx.User.Id, bet, guess?.Trim());
            var eb = CreateEmbed()
                .WithTitle("🎰 Roulette")
                .AddField("Result", result)
                .AddField("Outcome", won ? $"**WIN! +{payout} 🥠**" : $"**LOSE! -{bet} 🥠**")
                .WithColor(won ? Discord.Color.Green : Discord.Color.Red);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Blackjack(long bet)
        {
            if (bet < 1)
            {
                await Response().Error("Minimum bet is 1 🥠!").SendAsync();
                return;
            }

            var (won, result, payout) = await _service.BlackjackAsync(ctx.User.Id, bet);
            var eb = CreateEmbed()
                .WithTitle("🃏 Blackjack")
                .WithDescription(result)
                .WithColor(won ? Discord.Color.Green : Discord.Color.Red);

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PokerStart(long bet = 100)
        {
            if (bet < 10)
            {
                await Response().Error("Minimum bet is 10 🥠!").SendAsync();
                return;
            }

            var game = _service.GetOrCreatePokerGame(ctx.Channel.Id, ctx.User.Id, ctx.User.Username, bet);
            if (game.Players.Count > 1 || game.Players[0].UserId != ctx.User.Id)
            {
                await Response().Error("A poker game is already running in this channel!").SendAsync();
                return;
            }

            await Response().Confirm($"🃏 **Poker Game Started!** Bet: {bet} 🥠\nUse `.gamble pokerjoin` to join. `.gamble pokergo` to deal!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PokerJoin()
        {
            var joined = _service.JoinPokerGame(ctx.Channel.Id, ctx.User.Id, ctx.User.Username);
            if (joined)
                await Response().Confirm($"{ctx.User.Username} joined the poker game!").SendAsync();
            else
                await Response().Error("Couldn't join. No game, already in, or game is full (6 max).").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PokerGo()
        {
            var (winnerId, winnerName, pot, summary) = await _service.ResolvePokerAsync(ctx.Channel.Id);
            if (winnerId == 0)
            {
                await Response().Error(summary).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🃏 Poker Results")
                .WithDescription(summary)
                .WithFooter($"Winner: {winnerName} takes {pot} 🥠!")
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
