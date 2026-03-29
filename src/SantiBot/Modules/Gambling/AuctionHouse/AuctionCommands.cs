#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.AuctionHouse;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("Auction")]
    [Group("auction")]
    public partial class AuctionCommands : SantiModule<AuctionService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Create(string item, long startPrice, int durationHours = 24)
        {
            var (success, message) = await _service.CreateAuctionAsync(ctx.Guild.Id, ctx.User.Id, ctx.User.Username, item, startPrice, durationHours);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Bid(int id, long amount)
        {
            var (success, message) = await _service.BidAsync(ctx.Guild.Id, ctx.User.Id, ctx.User.Username, id, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task List()
        {
            var auctions = await _service.ListActiveAsync(ctx.Guild.Id);
            if (auctions.Count == 0)
            {
                await Response().Error("No active auctions!").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle("🏛️ Auction House")
                .WithOkColor();

            foreach (var a in auctions.Take(10))
            {
                var timeLeft = a.EndsAt - DateTime.UtcNow;
                eb.AddField(
                    $"#{a.Id} — {a.ItemDescription}",
                    $"Seller: {a.SellerName}\nCurrent Bid: {a.CurrentBid} 🥠\nTop Bidder: {(a.HighestBidderId == 0 ? "None" : a.HighestBidderName)}\nEnds in: {timeLeft.Hours}h {timeLeft.Minutes}m",
                    false);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task My()
        {
            var auctions = await _service.GetMyAuctionsAsync(ctx.Guild.Id, ctx.User.Id);
            if (auctions.Count == 0)
            {
                await Response().Error("You have no auctions!").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var a in auctions)
                sb.AppendLine($"#{a.Id} | {a.ItemDescription} | {a.CurrentBid} 🥠 | {(a.IsActive ? "Active" : "Ended")}");

            await Response().Confirm($"📦 Your Auctions\n```\n{sb}\n```").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cancel(int id)
        {
            var (success, message) = await _service.CancelAuctionAsync(ctx.Guild.Id, ctx.User.Id, id);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }
    }
}
