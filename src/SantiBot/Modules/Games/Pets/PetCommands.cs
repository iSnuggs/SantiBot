#nullable disable
using SantiBot.Modules.Games.Pets;
using SantiBot.Db.Models;
using System.Text;

namespace SantiBot.Modules.Games;

public partial class Games
{
    [Name("Pets")]
    [Group("pet")]
    public partial class PetCommands(ICurrencyProvider cp) : SantiModule<PetService>
    {
        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetAdopt([Leftover] string speciesName = null)
        {
            if (string.IsNullOrWhiteSpace(speciesName))
            {
                await Response().Error("Please specify a species to adopt! Use `.pet species` to see all options.").SendAsync();
                return;
            }

            var (pet, error) = await _service.AdoptPetAsync(ctx.User.Id, ctx.Guild.Id, speciesName.Trim());
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = cp.GetCurrencySign();
            var shinyText = pet.IsShiny ? " **\u2728 SHINY \u2728**" : "";
            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Emoji} Pet Adopted!{shinyText}")
                .WithDescription(
                    $"You adopted a **{pet.Species}**!\n\n"
                    + $"**Name:** {pet.Name}\n"
                    + $"**Level:** {pet.Level}\n"
                    + $"**STR:** {pet.Strength} | **AGI:** {pet.Agility} | **INT:** {pet.Intelligence}\n"
                    + $"**Pet ID:** {pet.Id}")
                .WithFooter($"Feed, play, and adventure to level up your pet!");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetFeed(int petId = 0)
        {
            if (petId <= 0)
            {
                var first = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (first is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
                petId = first.Id;
            }

            var (pet, error) = await _service.FeedPetAsync(ctx.User.Id, ctx.Guild.Id, petId);
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Emoji} {pet.Name} was fed!")
                .WithDescription(
                    $"**Hunger:** {HungerBar(pet.Hunger)} {pet.Hunger}/100\n"
                    + $"**Happiness:** {HappinessBar(pet.Happiness)} {pet.Happiness}/100\n"
                    + $"**Level:** {pet.Level} | **XP:** {pet.Xp}\n"
                    + (pet.EvolutionStage > 1 ? $"\u2728 Evolution Stage: {pet.EvolutionStage}\n" : ""));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetPlay(int petId = 0)
        {
            if (petId <= 0)
            {
                var first = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (first is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
                petId = first.Id;
            }

            var (pet, error) = await _service.PlayWithPetAsync(ctx.User.Id, ctx.Guild.Id, petId);
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Emoji} {pet.Name} had fun playing!")
                .WithDescription(
                    $"**Happiness:** {HappinessBar(pet.Happiness)} {pet.Happiness}/100\n"
                    + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100\n"
                    + $"**Level:** {pet.Level} | **XP:** {pet.Xp}\n"
                    + (pet.EvolutionStage > 1 ? $"\u2728 Evolution Stage: {pet.EvolutionStage}\n" : ""));

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetRest(int petId = 0)
        {
            if (petId <= 0)
            {
                var first = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (first is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
                petId = first.Id;
            }

            var (pet, energyGained, error) = await _service.RestPetAsync(ctx.User.Id, ctx.Guild.Id, petId);
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Emoji} {pet.Name} took a nap!")
                .WithDescription(
                    $"Recovered **+{energyGained}** energy!\n\n"
                    + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task Pet(int petId = 0)
        {
            Pet pet;
            if (petId <= 0)
            {
                pet = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (pet is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
            }
            else
            {
                pet = await _service.GetPetAsync(ctx.User.Id, ctx.Guild.Id, petId);
                if (pet is null)
                {
                    await Response().Error("You don't have a pet with that ID!").SendAsync();
                    return;
                }
            }

            var species = _service.GetSpeciesData(pet.Species);
            var rarityText = species is not null ? species.Rarity.ToString() : "Unknown";
            var shinyText = pet.IsShiny ? " \u2728 SHINY" : "";

            var evoText = pet.EvolutionStage switch
            {
                1 => "Baby",
                2 => "Juvenile",
                3 => "Adult",
                _ => "Unknown",
            };

            var nextEvoText = pet.EvolutionStage switch
            {
                1 => $"Next evolution at level 10",
                2 => $"Next evolution at level 25",
                3 => "Fully evolved!",
                _ => "",
            };

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"{pet.Emoji} {pet.Name}{shinyText}")
                .WithDescription($"**{pet.Species}** | {rarityText} | {evoText} (Stage {pet.EvolutionStage}/3)")
                .AddField("Level", $"{pet.Level} ({pet.Xp} XP)", true)
                .AddField("Stats", $"STR: {pet.Strength} | AGI: {pet.Agility} | INT: {pet.Intelligence}", true)
                .AddField("Hunger", $"{HungerBar(pet.Hunger)} {pet.Hunger}/100", false)
                .AddField("Happiness", $"{HappinessBar(pet.Happiness)} {pet.Happiness}/100", false)
                .AddField("Energy", $"{EnergyBar(pet.Energy)} {pet.Energy}/100", false)
                .AddField("Lifetime", $"Adventures: {pet.AdventureCount} | Battles Won: {pet.BattlesWon}", false)
                .WithFooter($"Pet ID: {pet.Id} | {nextEvoText}");

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetList()
        {
            var pets = await _service.GetAllPetsAsync(ctx.User.Id, ctx.Guild.Id);
            if (pets.Count == 0)
            {
                await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                return;
            }

            var sb = new StringBuilder();
            foreach (var pet in pets.OrderByDescending(p => p.Level))
            {
                var shinyMark = pet.IsShiny ? " \u2728" : "";
                sb.AppendLine($"**{pet.Emoji} {pet.Name}**{shinyMark} (ID: {pet.Id})");
                sb.AppendLine($"  Lv.{pet.Level} {pet.Species} | STR:{pet.Strength} AGI:{pet.Agility} INT:{pet.Intelligence}");
                sb.AppendLine($"  \ud83c\udf56 {pet.Hunger}/100 | \ud83d\ude04 {pet.Happiness}/100 | \u26a1 {pet.Energy}/100");
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"\ud83d\udc3e {ctx.User.Username}'s Pets ({pets.Count})")
                .WithDescription(sb.ToString());

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetRename(int petId, [Leftover] string newName = null)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                await Response().Error("Please provide a new name! Usage: `.pet rename <id> <name>`").SendAsync();
                return;
            }

            var (pet, error) = await _service.RenamePetAsync(ctx.User.Id, ctx.Guild.Id, petId, newName.Trim());
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{pet.Emoji} Your pet has been renamed to **{pet.Name}**!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetRelease(int petId)
        {
            var (pet, error) = await _service.ReleasePetAsync(ctx.User.Id, ctx.Guild.Id, petId);
            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            await Response().Confirm($"{pet.Emoji} **{pet.Name}** has been released into the wild. Goodbye, little friend!").SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetAdventure(int petId = 0)
        {
            if (petId <= 0)
            {
                var first = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (first is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
                petId = first.Id;
            }

            var (success, narrative, currencyEarned, xpEarned, pet, error) =
                await _service.AdventureAsync(ctx.User.Id, ctx.Guild.Id, petId);

            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = cp.GetCurrencySign();
            var eb = CreateEmbed();

            if (success)
            {
                eb.WithOkColor()
                    .WithTitle($"\ud83c\udfc6 Adventure Success!")
                    .WithDescription(
                        $"{narrative}\n\n"
                        + $"**Earned:** {currencyEarned}{sign} | **+{xpEarned} XP**\n"
                        + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100\n"
                        + $"**Level:** {pet.Level} | **Adventures:** {pet.AdventureCount}");
            }
            else
            {
                eb.WithErrorColor()
                    .WithTitle($"\ud83d\udca8 Adventure Failed!")
                    .WithDescription(
                        $"{narrative}\n\n"
                        + $"**+{xpEarned} XP** (consolation)\n"
                        + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100");
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetBattle(int petId = 0)
        {
            if (petId <= 0)
            {
                var first = await _service.GetFirstPetAsync(ctx.User.Id, ctx.Guild.Id);
                if (first is null)
                {
                    await Response().Error("You don't have any pets! Use `.pet adopt <species>` to get one.").SendAsync();
                    return;
                }
                petId = first.Id;
            }

            var (won, narrative, currencyEarned, xpEarned, enemyLevel, enemyName, pet, error) =
                await _service.BattleAsync(ctx.User.Id, ctx.Guild.Id, petId);

            if (pet is null)
            {
                await Response().Error(error).SendAsync();
                return;
            }

            var sign = cp.GetCurrencySign();
            var eb = CreateEmbed();

            if (won)
            {
                eb.WithOkColor()
                    .WithTitle($"\u2694\ufe0f Battle Won!")
                    .WithDescription(
                        $"**VS:** Lv.{enemyLevel} {enemyName}\n\n"
                        + $"{narrative}\n\n"
                        + $"**Earned:** {currencyEarned}{sign} | **+{xpEarned} XP**\n"
                        + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100\n"
                        + $"**Battles Won:** {pet.BattlesWon}");
            }
            else
            {
                eb.WithErrorColor()
                    .WithTitle($"\u2694\ufe0f Battle Lost!")
                    .WithDescription(
                        $"**VS:** Lv.{enemyLevel} {enemyName}\n\n"
                        + $"{narrative}\n\n"
                        + $"**+{xpEarned} XP** (consolation)\n"
                        + $"**Energy:** {EnergyBar(pet.Energy)} {pet.Energy}/100");
            }

            await Response().Embed(eb).SendAsync();
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async Task PetSpecies()
        {
            var sb = new StringBuilder();

            var grouped = PetService.AllSpecies
                .GroupBy(s => s.Rarity)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var rarityEmoji = group.Key switch
                {
                    PetRarity.Common => "\u26aa",
                    PetRarity.Uncommon => "\ud83d\udfe2",
                    PetRarity.Rare => "\ud83d\udd35",
                    PetRarity.Epic => "\ud83d\udfe3",
                    PetRarity.Legendary => "\ud83d\udfe1",
                    _ => "\u26aa",
                };

                var cost = group.Key switch
                {
                    PetRarity.Common => 100,
                    PetRarity.Uncommon => 500,
                    PetRarity.Rare => 1500,
                    PetRarity.Epic => 3000,
                    PetRarity.Legendary => 5000,
                    _ => 100,
                };

                sb.AppendLine($"### {rarityEmoji} {group.Key} ({cost} currency)");
                foreach (var species in group)
                {
                    sb.AppendLine($"{species.Emoji} **{species.Name}** — STR:{species.BaseStrength} AGI:{species.BaseAgility} INT:{species.BaseIntelligence}");
                    sb.AppendLine($"  Evo: {species.Name} \u2192 {species.Evo2Name} \u2192 {species.Evo3Name}");
                }
                sb.AppendLine();
            }

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle("\ud83d\udc3e Available Pet Species (30)")
                .WithDescription(sb.ToString())
                .WithFooter("Use .pet adopt <species name> to adopt!");

            await Response().Embed(eb).SendAsync();
        }

        private static string HungerBar(int value)
            => ProgressBar(value, "\ud83c\udf56");

        private static string HappinessBar(int value)
            => ProgressBar(value, "\ud83d\ude04");

        private static string EnergyBar(int value)
            => ProgressBar(value, "\u26a1");

        private static string ProgressBar(int value, string icon)
        {
            var filled = value / 10;
            var empty = 10 - filled;
            return $"{icon} {"".PadLeft(filled, '\u2588')}{"".PadLeft(empty, '\u2591')}";
        }
    }
}
