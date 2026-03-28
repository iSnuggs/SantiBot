#nullable disable
using SantiBot.Modules.Games.Crafting;
using System.Text;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Group("craft")]
    [Name("Crafting")]
    public partial class CraftingCommands(CraftingService cs) : SantiModule
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Craft()
        {
            var inventory = await cs.GetInventoryAsync(ctx.User.Id);
            var invLookup = inventory.ToDictionary(i => i.ItemId, i => i.Quantity);

            var sb = new StringBuilder();
            sb.AppendLine("Use `.craft \"Item Name\"` to craft an item.\n");

            foreach (var recipe in CraftingService.AllRecipes)
            {
                var canCraft = recipe.Materials.All(m =>
                    invLookup.TryGetValue(m.ItemId, out var qty) && qty >= m.Quantity);

                var status = canCraft ? "+" : "-";
                var sellInfo = recipe.SellValue > 0 ? $" (sells for {recipe.SellValue})" : "";
                sb.AppendLine($"[{status}] **{recipe.ResultName}**{sellInfo}");
                sb.AppendLine($"  {recipe.Description}");

                foreach (var mat in recipe.Materials)
                {
                    var have = invLookup.TryGetValue(mat.ItemId, out var q) ? q : 0;
                    var check = have >= mat.Quantity ? "+" : "x";
                    sb.AppendLine($"    [{check}] {mat.Quantity}x {mat.ItemName} (have: {have})");
                }
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Crafting Recipes")
                .WithDescription(sb.ToString())
                .WithFooter("[+] = ready to craft | [-] = missing materials");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Craft([Leftover] string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                await Craft();
                return;
            }

            // Strip quotes if present
            itemName = itemName.Trim('"', '\'');

            var (success, error, recipe) = await cs.CraftAsync(ctx.User.Id, itemName);

            if (!success)
            {
                if (error == "recipe_not_found")
                {
                    await Response().Error(strs.craft_recipe_not_found).SendAsync();
                    return;
                }

                // Missing material
                var missingId = error.Replace("missing_", "");
                var mat = recipe?.Materials.FirstOrDefault(m => m.ItemId == missingId);
                var matName = mat?.ItemName ?? missingId;
                await Response().Error(strs.craft_missing_materials(matName)).SendAsync();
                return;
            }

            var valueInfo = recipe.SellValue > 0
                ? $"\nYou received **{recipe.SellValue}** currency!"
                : "";

            await Response().Confirm(strs.craft_success(recipe.ResultName, valueInfo)).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Inventory()
        {
            var inventory = await cs.GetInventoryAsync(ctx.User.Id);

            if (inventory.Count == 0)
            {
                await Response().Error(strs.craft_inventory_empty).SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in inventory.OrderBy(i => i.ItemId))
            {
                // Try to find a friendly name from recipes
                var friendlyName = CraftingService.AllRecipes
                    .SelectMany(r => r.Materials)
                    .FirstOrDefault(m => m.ItemId == item.ItemId)?.ItemName
                    ?? CraftingService.AllRecipes
                        .FirstOrDefault(r => r.ResultId == item.ItemId)?.ResultName
                    ?? item.ItemId;

                sb.AppendLine($"**{friendlyName}** x{item.Quantity}");
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ctx.User.Username}'s Inventory")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CraftRecipes()
        {
            var sb = new StringBuilder();
            foreach (var recipe in CraftingService.AllRecipes)
            {
                var mats = string.Join(" + ", recipe.Materials.Select(m => $"{m.Quantity}x {m.ItemName}"));
                var sellInfo = recipe.SellValue > 0 ? $" | Sell: {recipe.SellValue}" : " | Collectible";
                sb.AppendLine($"**{recipe.ResultName}**{sellInfo}");
                sb.AppendLine($"  {mats}");
                sb.AppendLine($"  *{recipe.Description}*");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("All Crafting Recipes")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
