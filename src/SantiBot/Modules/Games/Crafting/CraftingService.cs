#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Crafting;

public sealed class CraftingService(DbService _db, ICurrencyService _cs) : INService
{
    public record MaterialReq(string ItemId, string ItemName, int Quantity);
    public record Recipe(string ResultId, string ResultName, string Description, long SellValue, MaterialReq[] Materials);

    public static readonly Recipe[] AllRecipes =
    [
        new("lucky_charm", "Lucky Charm", "A shimmering charm that brings good fortune", 5000,
        [
            new("fish_scales", "Fish Scales", 3),
            new("gold_nugget", "Gold Nugget", 1),
        ]),

        new("fishing_rod_upgrade", "Fishing Rod Upgrade", "Improves your fishing capabilities", 0,
        [
            new("wood", "Wood", 5),
            new("iron", "Iron", 2),
        ]),

        new("mythic_badge", "Mythic Badge", "An incredibly rare collectible badge", 25000,
        [
            new("dragon_scale", "Dragon Scale", 1),
            new("phoenix_feather", "Phoenix Feather", 1),
        ]),

        new("healing_potion", "Healing Potion", "Restores your energy for another adventure", 2000,
        [
            new("herb", "Herb", 3),
            new("crystal_shard", "Crystal Shard", 1),
        ]),

        new("treasure_map", "Treasure Map", "Reveals hidden currency stashes", 10000,
        [
            new("old_parchment", "Old Parchment", 2),
            new("compass_piece", "Compass Piece", 3),
            new("gold_nugget", "Gold Nugget", 2),
        ]),

        new("enchanted_bait", "Enchanted Bait", "Increases chance of rare fish", 3000,
        [
            new("herb", "Herb", 2),
            new("fish_scales", "Fish Scales", 5),
            new("crystal_shard", "Crystal Shard", 1),
        ]),
    ];

    /// <summary>
    /// Get all items in a user's crafting inventory.
    /// </summary>
    public async Task<List<CraftingInventory>> GetInventoryAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<CraftingInventory>()
            .Where(x => x.UserId == userId && x.Quantity > 0)
            .ToListAsyncLinqToDB();
    }

    /// <summary>
    /// Add materials to a user's inventory.
    /// </summary>
    public async Task AddMaterialAsync(ulong userId, string itemId, int quantity = 1)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<CraftingInventory>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (existing is not null)
        {
            await ctx.GetTable<CraftingInventory>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(_ => new CraftingInventory
                {
                    Quantity = existing.Quantity + quantity,
                });
        }
        else
        {
            ctx.Set<CraftingInventory>().Add(new CraftingInventory
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = quantity,
            });
            await ctx.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Attempt to craft an item. Returns (success, errorReason).
    /// </summary>
    public async Task<(bool success, string error, Recipe recipe)> CraftAsync(ulong userId, string recipeName)
    {
        var recipe = AllRecipes.FirstOrDefault(r =>
            r.ResultName.Equals(recipeName, StringComparison.OrdinalIgnoreCase)
            || r.ResultId.Equals(recipeName, StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
            return (false, "recipe_not_found", null);

        await using var ctx = _db.GetDbContext();

        // Check if user has all required materials
        foreach (var mat in recipe.Materials)
        {
            var inv = await ctx.GetTable<CraftingInventory>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == mat.ItemId);

            if (inv is null || inv.Quantity < mat.Quantity)
                return (false, $"missing_{mat.ItemId}", recipe);
        }

        // Consume materials
        foreach (var mat in recipe.Materials)
        {
            var inv = await ctx.GetTable<CraftingInventory>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == mat.ItemId);

            var remaining = inv.Quantity - mat.Quantity;
            if (remaining <= 0)
            {
                await ctx.GetTable<CraftingInventory>()
                    .Where(x => x.Id == inv.Id)
                    .DeleteAsync();
            }
            else
            {
                await ctx.GetTable<CraftingInventory>()
                    .Where(x => x.Id == inv.Id)
                    .UpdateAsync(_ => new CraftingInventory { Quantity = remaining });
            }
        }

        // Add crafted item to inventory
        var craftedExisting = await ctx.GetTable<CraftingInventory>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == recipe.ResultId);

        if (craftedExisting is not null)
        {
            await ctx.GetTable<CraftingInventory>()
                .Where(x => x.Id == craftedExisting.Id)
                .UpdateAsync(_ => new CraftingInventory
                {
                    Quantity = craftedExisting.Quantity + 1,
                });
        }
        else
        {
            ctx.Set<CraftingInventory>().Add(new CraftingInventory
            {
                UserId = userId,
                ItemId = recipe.ResultId,
                Quantity = 1,
            });
            await ctx.SaveChangesAsync();
        }

        // If the recipe has a sell value, add currency
        if (recipe.SellValue > 0)
            await _cs.AddAsync(userId, recipe.SellValue, new("crafting", "craft", $"Crafted {recipe.ResultName}"));

        return (true, null, recipe);
    }

    /// <summary>
    /// Get a recipe by name.
    /// </summary>
    public static Recipe GetRecipe(string name)
        => AllRecipes.FirstOrDefault(r =>
            r.ResultName.Equals(name, StringComparison.OrdinalIgnoreCase)
            || r.ResultId.Equals(name, StringComparison.OrdinalIgnoreCase));
}
