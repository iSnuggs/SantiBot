#nullable disable
using System.Text;
using SantiBot.Modules.Games.CardCollecting;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Cards")]
    [Group("cards")]
    public partial class CardCommands : SantiModule<CardService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CardDaily()
        {
            var (name, set, rarity, error) = await _service.DrawDailyCardAsync(ctx.User.Id);
            if (error is not null)
            {
                await Response().Error(error).SendAsync();
                return;
            }
            var emoji = rarity switch
            {
                "Common" => "⬜",
                "Uncommon" => "🟩",
                "Rare" => "🟦",
                "Epic" => "🟪",
                "Legendary" => "🟨",
                _ => "⬜"
            };

            var eb = CreateEmbed()
                .WithTitle("🃏 Daily Card!")
                .AddField("Card", name, true)
                .AddField("Set", set, true)
                .AddField("Rarity", $"{emoji} {rarity}", true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Inventory()
        {
            var cards = await _service.GetInventoryAsync(ctx.User.Id);
            if (cards.Count == 0)
            {
                await Response().Error("No cards! Use `.cards daily` to get your first card.").SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithTitle($"🃏 {ctx.User.Username}'s Cards ({cards.Count} unique)")
                .WithOkColor();

            var grouped = cards.GroupBy(c => c.Set);
            foreach (var set in grouped)
            {
                var sb = new StringBuilder();
                foreach (var c in set)
                {
                    var emoji = c.Rarity switch
                    {
                        "Common" => "⬜",
                        "Uncommon" => "🟩",
                        "Rare" => "🟦",
                        "Epic" => "🟪",
                        "Legendary" => "🟨",
                        _ => "⬜"
                    };
                    sb.AppendLine($"{emoji} {c.CardName} x{c.Quantity}");
                }
                eb.AddField($"📂 {set.Key}", sb.ToString(), true);
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Trade(IUser user, string offerCard, string wantCard)
        {
            var (success, message) = await _service.TradeCardAsync(ctx.User.Id, user.Id, offerCard, wantCard);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Album()
        {
            var cards = await _service.GetInventoryAsync(ctx.User.Id);
            var progress = _service.GetAlbumProgress(cards);

            var eb = CreateEmbed()
                .WithTitle("📖 Card Album Progress")
                .WithOkColor();

            foreach (var (setName, (have, total)) in progress)
            {
                var pct = total > 0 ? have * 100 / total : 0;
                var bar = new string('█', pct / 10) + new string('░', 10 - pct / 10);
                eb.AddField(setName, $"{bar} {have}/{total} ({pct}%)", false);
            }

            await Response().Embed(eb).SendAsync();
        }
    }
}
