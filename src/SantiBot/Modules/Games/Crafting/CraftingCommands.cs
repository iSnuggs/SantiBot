#nullable disable
using SantiBot.Modules.Games.Crafting;
using System.Text;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Crafting")]
    [Group("craft")]
    public partial class CraftingCommands : SantiModule<CraftingService>
    {
        // ─── Gathering Commands ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Mine()
        {
            var (success, error, drop, qty, newLevel, leveledUp, cooldownLeft) =
                await _service.MineAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success && error == "cooldown")
            {
                await Response().Error($"Your pickaxe is still dull! Try again in **{cooldownLeft.Value.Seconds}s**.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You swing your pickaxe and find **{qty}x {drop.ItemName}** ({drop.Rarity})!");
            sb.AppendLine($"Mining XP gained: **+{drop.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your Mining skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Mining")
                .WithDescription(sb.ToString())
                .WithFooter("60s cooldown between mining attempts");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Chop()
        {
            var (success, error, drop, qty, newLevel, leveledUp, cooldownLeft) =
                await _service.ChopAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success && error == "cooldown")
            {
                await Response().Error($"Your axe needs sharpening! Try again in **{cooldownLeft.Value.Seconds}s**.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You chop down a tree and gather **{qty}x {drop.ItemName}** ({drop.Rarity})!");
            sb.AppendLine($"Woodcutting XP gained: **+{drop.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your Woodcutting skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Woodcutting")
                .WithDescription(sb.ToString())
                .WithFooter("60s cooldown between chopping attempts");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Farm()
        {
            var (success, error, drop, qty, newLevel, leveledUp, cooldownLeft) =
                await _service.FarmAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success && error == "cooldown")
            {
                await Response().Error($"Your crops aren't ready yet! Try again in **{cooldownLeft.Value.Seconds}s**.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You tend your fields and harvest **{qty}x {drop.ItemName}** ({drop.Rarity})!");
            sb.AppendLine($"Farming XP gained: **+{drop.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your Farming skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Farming")
                .WithDescription(sb.ToString())
                .WithFooter("60s cooldown between harvests");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GatherHerbs()
        {
            var (success, error, drop, qty, newLevel, leveledUp, cooldownLeft) =
                await _service.GatherHerbsAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success && error == "cooldown")
            {
                await Response().Error($"The herb patches need time to regrow! Try again in **{cooldownLeft.Value.Seconds}s**.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You forage through the wilds and find **{qty}x {drop.ItemName}** ({drop.Rarity})!");
            sb.AppendLine($"Herb Gathering XP gained: **+{drop.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your Herb Gathering skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Herb Gathering")
                .WithDescription(sb.ToString())
                .WithFooter("60s cooldown between gathering attempts");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CraftFish()
        {
            var (success, error, drop, qty, newLevel, leveledUp, cooldownLeft) =
                await _service.FishGatherAsync(ctx.User.Id, ctx.Guild.Id);

            if (!success && error == "cooldown")
            {
                await Response().Error($"The fish aren't biting yet! Try again in **{cooldownLeft.Value.Seconds}s**.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You cast your line and reel in **{qty}x {drop.ItemName}** ({drop.Rarity})!");
            sb.AppendLine($"Fishing XP gained: **+{drop.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your Fishing skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("Fishing (Crafting)")
                .WithDescription(sb.ToString())
                .WithFooter("60s cooldown between fishing attempts");

            await Response().Embed(eb).SendAsync();
        }

        // ─── Crafting Commands ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Cook([Leftover] string recipeName = null)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                await ShowRecipes("cook");
                return;
            }

            recipeName = recipeName.Trim('"', '\'');
            var (success, error, recipe, leveledUp, newLevel) =
                await _service.CraftItemAsync(ctx.User.Id, ctx.Guild.Id, recipeName, "Cooking");

            await HandleCraftResult(success, error, recipe, leveledUp, newLevel, "Cooking");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Brew([Leftover] string recipeName = null)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                await ShowRecipes("brew");
                return;
            }

            recipeName = recipeName.Trim('"', '\'');
            var (success, error, recipe, leveledUp, newLevel) =
                await _service.CraftItemAsync(ctx.User.Id, ctx.Guild.Id, recipeName, "Alchemy");

            await HandleCraftResult(success, error, recipe, leveledUp, newLevel, "Alchemy");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Forge([Leftover] string recipeName = null)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                await ShowRecipes("forge");
                return;
            }

            recipeName = recipeName.Trim('"', '\'');
            var (success, error, recipe, leveledUp, newLevel) =
                await _service.CraftItemAsync(ctx.User.Id, ctx.Guild.Id, recipeName, "Blacksmithing");

            await HandleCraftResult(success, error, recipe, leveledUp, newLevel, "Blacksmithing");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Enchant([Leftover] string recipeName = null)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                await ShowRecipes("enchant");
                return;
            }

            recipeName = recipeName.Trim('"', '\'');
            var (success, error, recipe, leveledUp, newLevel) =
                await _service.CraftItemAsync(ctx.User.Id, ctx.Guild.Id, recipeName, "Enchanting");

            await HandleCraftResult(success, error, recipe, leveledUp, newLevel, "Enchanting");
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CraftJewelry([Leftover] string recipeName = null)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
            {
                await ShowRecipes("jewelry");
                return;
            }

            recipeName = recipeName.Trim('"', '\'');
            var (success, error, recipe, leveledUp, newLevel) =
                await _service.CraftItemAsync(ctx.User.Id, ctx.Guild.Id, recipeName, "Jewelcrafting");

            await HandleCraftResult(success, error, recipe, leveledUp, newLevel, "Jewelcrafting");
        }

        // ─── Info Commands ───

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Inventory()
        {
            var items = await _service.GetInventoryAsync(ctx.User.Id, ctx.Guild.Id);

            if (items.Count == 0)
            {
                await Response().Error("Your inventory is empty! Try `.craft mine` or `.craft chop` to gather materials.").SendAsync();
                return;
            }

            var grouped = items
                .OrderBy(i => i.ItemType)
                .ThenBy(i => i.ItemName)
                .GroupBy(i => i.ItemType);

            var sb = new StringBuilder();
            foreach (var group in grouped)
            {
                sb.AppendLine($"**--- {group.Key} ---**");
                foreach (var item in group)
                    sb.AppendLine($"  {item.ItemName} x{item.Quantity} ({item.Rarity})");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ctx.User.Username}'s Crafting Inventory")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Recipes([Leftover] string skill = null)
        {
            if (string.IsNullOrWhiteSpace(skill))
            {
                await Response().Error("Specify a skill: `cook`, `brew`, `forge`, `enchant`, or `jewelry`.").SendAsync();
                return;
            }

            await ShowRecipes(skill.Trim());
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task GatheringStats()
        {
            var profile = await _service.GetOrCreateGatheringProfileAsync(ctx.User.Id, ctx.Guild.Id);

            string Bar(int level, int xp)
            {
                var needed = CraftingService.XpForLevel(level);
                var pct = level >= 50 ? 100 : (int)((double)xp / needed * 100);
                var filled = pct / 10;
                return $"[{'#'.ToString().PadLeft(filled, '#').PadRight(10, '-')}] {pct}%";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"**Mining** Lv.**{profile.MiningLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.MiningLevel, profile.MiningXp)}`");
            sb.AppendLine($"  XP: {profile.MiningXp}/{CraftingService.XpForLevel(profile.MiningLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Woodcutting** Lv.**{profile.WoodcuttingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.WoodcuttingLevel, profile.WoodcuttingXp)}`");
            sb.AppendLine($"  XP: {profile.WoodcuttingXp}/{CraftingService.XpForLevel(profile.WoodcuttingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Farming** Lv.**{profile.FarmingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.FarmingLevel, profile.FarmingXp)}`");
            sb.AppendLine($"  XP: {profile.FarmingXp}/{CraftingService.XpForLevel(profile.FarmingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Fishing** Lv.**{profile.FishingSkillLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.FishingSkillLevel, profile.FishingSkillXp)}`");
            sb.AppendLine($"  XP: {profile.FishingSkillXp}/{CraftingService.XpForLevel(profile.FishingSkillLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Herb Gathering** Lv.**{profile.HerbGatheringLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.HerbGatheringLevel, profile.HerbGatheringXp)}`");
            sb.AppendLine($"  XP: {profile.HerbGatheringXp}/{CraftingService.XpForLevel(profile.HerbGatheringLevel)}");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ctx.User.Username}'s Gathering Skills")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task CraftingStats()
        {
            var profile = await _service.GetOrCreateCraftingProfileAsync(ctx.User.Id, ctx.Guild.Id);

            string Bar(int level, int xp)
            {
                var needed = CraftingService.XpForLevel(level);
                var pct = level >= 50 ? 100 : (int)((double)xp / needed * 100);
                var filled = pct / 10;
                return $"[{'#'.ToString().PadLeft(filled, '#').PadRight(10, '-')}] {pct}%";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"**Cooking** Lv.**{profile.CookingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.CookingLevel, profile.CookingXp)}`");
            sb.AppendLine($"  XP: {profile.CookingXp}/{CraftingService.XpForLevel(profile.CookingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Alchemy** Lv.**{profile.AlchemyLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.AlchemyLevel, profile.AlchemyXp)}`");
            sb.AppendLine($"  XP: {profile.AlchemyXp}/{CraftingService.XpForLevel(profile.AlchemyLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Blacksmithing** Lv.**{profile.BlacksmithingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.BlacksmithingLevel, profile.BlacksmithingXp)}`");
            sb.AppendLine($"  XP: {profile.BlacksmithingXp}/{CraftingService.XpForLevel(profile.BlacksmithingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Enchanting** Lv.**{profile.EnchantingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.EnchantingLevel, profile.EnchantingXp)}`");
            sb.AppendLine($"  XP: {profile.EnchantingXp}/{CraftingService.XpForLevel(profile.EnchantingLevel)}");
            sb.AppendLine();
            sb.AppendLine($"**Jewelcrafting** Lv.**{profile.JewelcraftingLevel}**/50");
            sb.AppendLine($"  `{Bar(profile.JewelcraftingLevel, profile.JewelcraftingXp)}`");
            sb.AppendLine($"  XP: {profile.JewelcraftingXp}/{CraftingService.XpForLevel(profile.JewelcraftingLevel)}");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{ctx.User.Username}'s Crafting Skills")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        // ─── Helper Methods ───

        private async Task ShowRecipes(string skill)
        {
            var recipes = CraftingService.GetRecipesForSkill(skill);
            if (recipes.Length == 0)
            {
                await Response().Error("Unknown skill. Use: `cook`, `brew`, `forge`, `enchant`, or `jewelry`.").SendAsync();
                return;
            }

            var skillName = recipes[0].Skill;
            var inventory = await _service.GetInventoryAsync(ctx.User.Id, ctx.Guild.Id);
            var invLookup = inventory.ToDictionary(i => i.ItemName, i => i.Quantity);

            var sb = new StringBuilder();
            sb.AppendLine($"Use `.craft {skill.ToLower()} \"Recipe Name\"` to craft.\n");

            foreach (var recipe in recipes)
            {
                var canCraft = recipe.Materials.All(m =>
                    invLookup.TryGetValue(m.ItemName, out var qty) && qty >= m.Quantity);

                var status = canCraft ? "+" : "-";
                sb.AppendLine($"[{status}] **{recipe.Name}** (Lv.{recipe.RequiredLevel}) — {recipe.ResultRarity}");

                foreach (var mat in recipe.Materials)
                {
                    var have = invLookup.TryGetValue(mat.ItemName, out var q) ? q : 0;
                    var check = have >= mat.Quantity ? "+" : "x";
                    sb.AppendLine($"    [{check}] {mat.Quantity}x {mat.ItemName} (have: {have})");
                }
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{skillName} Recipes")
                .WithDescription(sb.ToString())
                .WithFooter("[+] = can craft | [-] = missing materials or level");

            await Response().Embed(eb).SendAsync();
        }

        private async Task HandleCraftResult(bool success, string error,
            CraftingService.CraftRecipe recipe, bool leveledUp, int newLevel, string skillName)
        {
            if (!success)
            {
                if (error == "recipe_not_found")
                {
                    await Response().Error($"Recipe not found! Use `.craft recipes {skillName.ToLower()}` to see available recipes.").SendAsync();
                    return;
                }

                if (error.StartsWith("need_level_"))
                {
                    var reqLevel = error.Replace("need_level_", "");
                    await Response().Error($"You need **{skillName}** level **{reqLevel}** to craft **{recipe.Name}**!").SendAsync();
                    return;
                }

                if (error.StartsWith("missing_"))
                {
                    var matName = error.Replace("missing_", "");
                    await Response().Error($"You don't have enough **{matName}** to craft **{recipe.Name}**!").SendAsync();
                    return;
                }

                await Response().Error("Something went wrong with crafting.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"You crafted **{recipe.ResultItem}** ({recipe.ResultRarity})!");
            sb.AppendLine($"{skillName} XP gained: **+{recipe.XpReward}** | Level: **{newLevel}**/50");
            if (leveledUp)
                sb.AppendLine($"**LEVEL UP!** Your {skillName} skill is now level **{newLevel}**!");

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{skillName} — Crafted!")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }
    }
}
