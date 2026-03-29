#nullable disable
using System.Text;
using SantiBot.Modules.Gambling.LootBoxes;

namespace SantiBot.Modules.Gambling;

public partial class Gambling
{
    [Name("LootBox")]
    [Group("lootbox")]
    public partial class LootBoxCommands : SantiModule<LootBoxService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Open(int amount = 1)
        {
            var (success, message, results) = await _service.OpenBoxesAsync(ctx.Guild.Id, ctx.User.Id, amount);
            if (!success)
            {
                await Response().Error(message).SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var (tier, reward) in results)
            {
                var emoji = tier switch
                {
                    "Common" => "⬜",
                    "Uncommon" => "🟩",
                    "Rare" => "🟦",
                    "Legendary" => "🟨",
                    "Mythic" => "🟪",
                    _ => "⬜"
                };
                sb.AppendLine($"{emoji} **{tier}** — {reward} 🥠");
            }

            var eb = CreateEmbed()
                .WithTitle("🎁 Loot Box Results")
                .WithDescription(sb.ToString())
                .WithFooter(message)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Buy(int amount = 1)
        {
            var (success, message) = await _service.BuyBoxesAsync(ctx.Guild.Id, ctx.User.Id, amount);
            if (success)
                await Response().Confirm(message).SendAsync();
            else
                await Response().Error(message).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Inventory()
        {
            var inv = await _service.GetInventoryAsync(ctx.Guild.Id, ctx.User.Id);

            var eb = CreateEmbed()
                .WithTitle($"📦 {ctx.User.Username}'s Loot Boxes")
                .AddField("Unopened", inv.UnopenedBoxes.ToString(), true)
                .AddField("⬜ Common", inv.CommonBoxes.ToString(), true)
                .AddField("🟩 Uncommon", inv.UncommonBoxes.ToString(), true)
                .AddField("🟦 Rare", inv.RareBoxes.ToString(), true)
                .AddField("🟨 Legendary", inv.LegendaryBoxes.ToString(), true)
                .AddField("🟪 Mythic", inv.MythicBoxes.ToString(), true)
                .WithOkColor();

            await Response().Embed(eb).SendAsync();
        }
    }
}
