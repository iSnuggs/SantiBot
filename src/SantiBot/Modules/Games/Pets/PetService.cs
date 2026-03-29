#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Pets;

public sealed class PetService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();

    public static readonly PetSpeciesData[] AllSpecies =
    [
        new("Dog", "\ud83d\udc36", 8, 6, 5, "Loyal Hound", "\ud83d\udc15", "Alpha Wolf-Dog", "\ud83e\udd3a", PetRarity.Common),
        new("Cat", "\ud83d\udc31", 5, 8, 7, "Sleek Feline", "\ud83d\udc08", "Shadow Panther", "\ud83d\udc08\u200d\u2b1b", PetRarity.Common),
        new("Fox", "\ud83e\udd8a", 6, 9, 6, "Swift Fox", "\ud83e\udd8a", "Nine-Tail Fox", "\ud83e\udd8a", PetRarity.Common),
        new("Rabbit", "\ud83d\udc30", 4, 10, 5, "Fleet Hare", "\ud83d\udc07", "Lunar Rabbit", "\ud83c\udf19", PetRarity.Common),
        new("Hamster", "\ud83d\udc39", 3, 7, 6, "Chubby Hamster", "\ud83d\udc39", "Golden Hamster King", "\ud83d\udc51", PetRarity.Common),
        new("Parrot", "\ud83e\udd9c", 5, 7, 8, "Clever Parrot", "\ud83e\udd9c", "Phoenix Parrot", "\ud83e\udd9c", PetRarity.Common),
        new("Owl", "\ud83e\udd89", 5, 6, 10, "Wise Owl", "\ud83e\udd89", "Grand Sage Owl", "\ud83e\uddd9", PetRarity.Uncommon),
        new("Penguin", "\ud83d\udc27", 6, 5, 7, "Emperor Penguin", "\ud83d\udc27", "Frost Emperor", "\u2744\ufe0f", PetRarity.Uncommon),
        new("Dragon Hatchling", "\ud83d\udc32", 9, 7, 8, "Young Dragon", "\ud83d\udc09", "Elder Dragon", "\ud83d\udc32", PetRarity.Epic),
        new("Phoenix Chick", "\ud83d\udc25", 7, 8, 9, "Rising Phoenix", "\ud83d\udd25", "Eternal Phoenix", "\ud83c\udf1f", PetRarity.Epic),
        new("Wolf Pup", "\ud83d\udc3a", 9, 8, 5, "Dire Wolf", "\ud83d\udc3a", "Fenrir", "\ud83c\udf11", PetRarity.Uncommon),
        new("Bear Cub", "\ud83d\udc3b", 10, 4, 5, "Grizzly Bear", "\ud83d\udc3b", "Armored War Bear", "\ud83d\udee1\ufe0f", PetRarity.Uncommon),
        new("Tiger Cub", "\ud83d\udc2f", 9, 9, 5, "Bengal Tiger", "\ud83d\udc05", "Saber-Tooth Tiger", "\u2694\ufe0f", PetRarity.Rare),
        new("Lion Cub", "\ud83e\udd81", 10, 7, 6, "Proud Lion", "\ud83e\udd81", "Lion King", "\ud83d\udc51", PetRarity.Rare),
        new("Panda", "\ud83d\udc3c", 8, 5, 7, "Giant Panda", "\ud83d\udc3c", "Kung Fu Panda", "\ud83e\udd4b", PetRarity.Uncommon),
        new("Turtle", "\ud83d\udc22", 7, 3, 8, "Ancient Turtle", "\ud83d\udc22", "World Turtle", "\ud83c\udf0d", PetRarity.Common),
        new("Snake", "\ud83d\udc0d", 7, 8, 6, "Viper", "\ud83d\udc0d", "Basilisk", "\ud83d\udc09", PetRarity.Common),
        new("Frog", "\ud83d\udc38", 4, 7, 6, "Tree Frog", "\ud83d\udc38", "Poison Dart Frog", "\u2620\ufe0f", PetRarity.Common),
        new("Butterfly", "\ud83e\udd8b", 3, 9, 7, "Monarch Butterfly", "\ud83e\udd8b", "Cosmic Butterfly", "\ud83c\udf0c", PetRarity.Common),
        new("Bee", "\ud83d\udc1d", 5, 8, 6, "Queen Bee", "\ud83d\udc1d", "Golden Empress Bee", "\ud83d\udc1d", PetRarity.Common),
        new("Bat", "\ud83e\udd87", 6, 9, 7, "Vampire Bat", "\ud83e\udd87", "Nightwing Bat", "\ud83c\udf11", PetRarity.Uncommon),
        new("Monkey", "\ud83d\udc12", 7, 8, 8, "Clever Ape", "\ud83d\udc35", "Sage Primate", "\ud83e\uddd0", PetRarity.Uncommon),
        new("Elephant Baby", "\ud83d\udc18", 10, 3, 7, "Bull Elephant", "\ud83d\udc18", "Mammoth", "\ud83e\udda3", PetRarity.Rare),
        new("Unicorn Foal", "\ud83e\udd84", 7, 8, 10, "Unicorn", "\ud83e\udd84", "Celestial Unicorn", "\u2728", PetRarity.Epic),
        new("Griffin Hatchling", "\ud83e\udd85", 9, 9, 7, "Young Griffin", "\ud83e\udd85", "Royal Griffin", "\ud83d\udc51", PetRarity.Epic),
        new("Slime", "\ud83e\udea8", 4, 5, 4, "Gel Cube", "\ud83d\udfe2", "Slime King", "\ud83d\udc51", PetRarity.Common),
        new("Ghost Pup", "\ud83d\udc7b", 5, 10, 8, "Phantom Hound", "\ud83d\udc7b", "Wraith Wolf", "\u2620\ufe0f", PetRarity.Rare),
        new("Robot Pet", "\ud83e\udd16", 8, 6, 10, "Mech Companion", "\ud83e\udd16", "Omega Android", "\ud83d\udcab", PetRarity.Rare),
        new("Crystal Golem", "\ud83d\udc8e", 10, 3, 8, "Gem Guardian", "\ud83d\udc8e", "Diamond Titan", "\ud83d\udc8e", PetRarity.Legendary),
        new("Shadow Cat", "\ud83d\udc08\u200d\u2b1b", 6, 10, 9, "Void Stalker", "\ud83c\udf11", "Abyssal Panther", "\ud83c\udf0c", PetRarity.Legendary),
    ];

    private static long GetAdoptCost(PetRarity rarity)
        => rarity switch
        {
            PetRarity.Common => 100,
            PetRarity.Uncommon => 500,
            PetRarity.Rare => 1500,
            PetRarity.Epic => 3000,
            PetRarity.Legendary => 5000,
            _ => 100,
        };

    private static long XpForLevel(int level)
        => 50L * level * level;

    private static void ApplyDecay(Pet pet)
    {
        var now = DateTime.UtcNow;

        var hoursSinceFed = (now - pet.LastFedAt).TotalHours;
        if (hoursSinceFed > 1)
        {
            var decay = (int)(hoursSinceFed * 3);
            pet.Hunger = Math.Max(0, pet.Hunger - decay);
        }

        var hoursSincePlayed = (now - pet.LastPlayedAt).TotalHours;
        if (hoursSincePlayed > 1)
        {
            var decay = (int)(hoursSincePlayed * 2);
            pet.Happiness = Math.Max(0, pet.Happiness - decay);
        }
    }

    private static void CheckLevelUp(Pet pet)
    {
        while (pet.Xp >= XpForLevel(pet.Level) && pet.Level < 50)
        {
            pet.Xp -= XpForLevel(pet.Level);
            pet.Level++;

            pet.Strength += _rng.Next(1, 4);
            pet.Agility += _rng.Next(1, 4);
            pet.Intelligence += _rng.Next(1, 4);

            if (pet.Level == 10 && pet.EvolutionStage == 1)
                pet.EvolutionStage = 2;
            else if (pet.Level == 25 && pet.EvolutionStage == 2)
                pet.EvolutionStage = 3;
        }
    }

    private static void ApplyEvolutionVisuals(Pet pet, PetSpeciesData species)
    {
        switch (pet.EvolutionStage)
        {
            case 2:
                pet.Name = species.Evo2Name;
                pet.Emoji = species.Evo2Emoji;
                break;
            case 3:
                pet.Name = species.Evo3Name;
                pet.Emoji = species.Evo3Emoji;
                break;
        }
    }

    public PetSpeciesData GetSpeciesData(string speciesName)
        => AllSpecies.FirstOrDefault(s =>
            s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));

    public async Task<Pet> GetPetAsync(ulong userId, ulong guildId, int petId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.GetTable<Pet>()
            .FirstOrDefaultAsync(p => p.Id == petId && p.UserId == userId && p.GuildId == guildId);

        if (pet is not null)
            ApplyDecay(pet);

        return pet;
    }

    public async Task<List<Pet>> GetAllPetsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var pets = await ctx.GetTable<Pet>()
            .Where(p => p.UserId == userId && p.GuildId == guildId)
            .ToListAsync();

        foreach (var pet in pets)
            ApplyDecay(pet);

        return pets;
    }

    public async Task<Pet> GetFirstPetAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.GetTable<Pet>()
            .Where(p => p.UserId == userId && p.GuildId == guildId)
            .OrderByDescending(p => p.Level)
            .FirstOrDefaultAsync();

        if (pet is not null)
            ApplyDecay(pet);

        return pet;
    }

    private async Task SavePetAsync(Pet pet)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<Pet>()
            .Where(p => p.Id == pet.Id)
            .UpdateAsync(_ => new Pet
            {
                Name = pet.Name,
                Emoji = pet.Emoji,
                Level = pet.Level,
                Xp = pet.Xp,
                Happiness = pet.Happiness,
                Hunger = pet.Hunger,
                Energy = pet.Energy,
                IsShiny = pet.IsShiny,
                Strength = pet.Strength,
                Agility = pet.Agility,
                Intelligence = pet.Intelligence,
                AdventureCount = pet.AdventureCount,
                BattlesWon = pet.BattlesWon,
                LastFedAt = pet.LastFedAt,
                LastPlayedAt = pet.LastPlayedAt,
                EvolutionStage = pet.EvolutionStage,
            });
    }

    public async Task<(Pet pet, string error)> AdoptPetAsync(ulong userId, ulong guildId, string speciesName)
    {
        var species = GetSpeciesData(speciesName);
        if (species is null)
            return (null, "That species doesn't exist! Use `.pet species` to see all available pets.");

        var cost = GetAdoptCost(species.Rarity);
        var taken = await _cs.RemoveAsync(userId, cost, new("pet", "adopt", $"Adopted a {species.Name}"));
        if (!taken)
            return (null, $"You need **{cost}** currency to adopt a {species.Name}!");

        var isShiny = _rng.Next(0, 100) < 3;

        var pet = new Pet
        {
            UserId = userId,
            GuildId = guildId,
            Name = species.Name,
            Species = species.Name,
            Emoji = species.Emoji,
            Level = 1,
            Xp = 0,
            Happiness = 50,
            Hunger = 50,
            Energy = 100,
            IsShiny = isShiny,
            Strength = species.BaseStrength + _rng.Next(0, 3),
            Agility = species.BaseAgility + _rng.Next(0, 3),
            Intelligence = species.BaseIntelligence + _rng.Next(0, 3),
            AdventureCount = 0,
            BattlesWon = 0,
            LastFedAt = DateTime.UtcNow,
            LastPlayedAt = DateTime.UtcNow,
            EvolutionStage = 1,
        };

        await using var ctx = _db.GetDbContext();
        ctx.Set<Pet>().Add(pet);
        await ctx.SaveChangesAsync();

        return (pet, null);
    }

    public async Task<(Pet pet, string error)> FeedPetAsync(ulong userId, ulong guildId, int petId)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (null, "You don't have a pet with that ID!");

        if (pet.Hunger >= 100)
            return (null, $"**{pet.Name}** is already full!");

        var taken = await _cs.RemoveAsync(userId, 10, new("pet", "feed", $"Fed {pet.Name}"));
        if (!taken)
            return (null, "You need **10** currency to feed your pet!");

        pet.Hunger = Math.Min(100, pet.Hunger + 25);
        pet.Xp += 10 + _rng.Next(0, 6);
        pet.LastFedAt = DateTime.UtcNow;

        if (pet.Hunger > 70)
            pet.Happiness = Math.Min(100, pet.Happiness + 5);

        var oldStage = pet.EvolutionStage;
        CheckLevelUp(pet);
        if (pet.EvolutionStage != oldStage)
        {
            var species = GetSpeciesData(pet.Species);
            if (species is not null)
                ApplyEvolutionVisuals(pet, species);
        }

        await SavePetAsync(pet);
        return (pet, null);
    }

    public async Task<(Pet pet, string error)> PlayWithPetAsync(ulong userId, ulong guildId, int petId)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (null, "You don't have a pet with that ID!");

        if (pet.Energy < 15)
            return (null, $"**{pet.Name}** is too tired to play! Let them rest first.");

        pet.Happiness = Math.Min(100, pet.Happiness + 20);
        pet.Energy = Math.Max(0, pet.Energy - 15);
        pet.Xp += 15 + _rng.Next(0, 11);
        pet.LastPlayedAt = DateTime.UtcNow;

        var oldStage = pet.EvolutionStage;
        CheckLevelUp(pet);
        if (pet.EvolutionStage != oldStage)
        {
            var species = GetSpeciesData(pet.Species);
            if (species is not null)
                ApplyEvolutionVisuals(pet, species);
        }

        await SavePetAsync(pet);
        return (pet, null);
    }

    public async Task<(Pet pet, int energyGained, string error)> RestPetAsync(ulong userId, ulong guildId, int petId)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (null, 0, "You don't have a pet with that ID!");

        if (pet.Energy >= 100)
            return (null, 0, $"**{pet.Name}** is already fully rested!");

        var gained = 20 + _rng.Next(0, 16);
        pet.Energy = Math.Min(100, pet.Energy + gained);
        pet.Xp += 5;

        var oldStage = pet.EvolutionStage;
        CheckLevelUp(pet);
        if (pet.EvolutionStage != oldStage)
        {
            var species = GetSpeciesData(pet.Species);
            if (species is not null)
                ApplyEvolutionVisuals(pet, species);
        }

        await SavePetAsync(pet);
        return (pet, gained, null);
    }

    public async Task<(Pet pet, string error)> RenamePetAsync(ulong userId, ulong guildId, int petId, string newName)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (null, "You don't have a pet with that ID!");

        if (string.IsNullOrWhiteSpace(newName) || newName.Length > 32)
            return (null, "Name must be between 1 and 32 characters!");

        pet.Name = newName;
        await SavePetAsync(pet);
        return (pet, null);
    }

    public async Task<(Pet pet, string error)> ReleasePetAsync(ulong userId, ulong guildId, int petId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.GetTable<Pet>()
            .FirstOrDefaultAsync(p => p.Id == petId && p.UserId == userId && p.GuildId == guildId);

        if (pet is null)
            return (null, "You don't have a pet with that ID!");

        await ctx.GetTable<Pet>()
            .Where(p => p.Id == pet.Id)
            .DeleteAsync();

        return (pet, null);
    }

    public async Task<(bool success, string narrative, long currencyEarned, long xpEarned, Pet pet, string error)>
        AdventureAsync(ulong userId, ulong guildId, int petId)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (false, null, 0, 0, null, "You don't have a pet with that ID!");

        if (pet.Energy < 25)
            return (false, null, 0, 0, null, $"**{pet.Name}** needs at least 25 energy to go on an adventure!");

        if (pet.Hunger < 20)
            return (false, null, 0, 0, null, $"**{pet.Name}** is too hungry to adventure! Feed them first.");

        pet.Energy = Math.Max(0, pet.Energy - 25);
        pet.Hunger = Math.Max(0, pet.Hunger - 10);
        pet.AdventureCount++;

        var totalStats = pet.Strength + pet.Agility + pet.Intelligence;
        var successChance = Math.Min(40 + totalStats, 95);
        var roll = _rng.Next(1, 101);
        var success = roll <= successChance;

        string narrative;
        long currencyEarned = 0;
        long xpEarned;

        if (success)
        {
            var narratives = new[]
            {
                $"**{pet.Name}** discovered a hidden treasure chest in the forest!",
                $"**{pet.Name}** rescued a lost traveler who rewarded them generously!",
                $"**{pet.Name}** found a rare crystal in a mountain cave!",
                $"**{pet.Name}** won a contest in the village square!",
                $"**{pet.Name}** stumbled upon an ancient ruin full of gold!",
                $"**{pet.Name}** helped a wizard and received a magical reward!",
                $"**{pet.Name}** defeated a band of goblins and claimed their loot!",
                $"**{pet.Name}** navigated a maze and found the prize at the center!",
            };
            narrative = narratives[_rng.Next(0, narratives.Length)];
            currencyEarned = 20 + (pet.Level * 5) + _rng.Next(0, 51);
            xpEarned = 25 + _rng.Next(0, 21);

            await _cs.AddAsync(userId, currencyEarned, new("pet", "adventure", $"{pet.Name} adventure reward"));
            pet.Happiness = Math.Min(100, pet.Happiness + 10);
        }
        else
        {
            var narratives = new[]
            {
                $"**{pet.Name}** got lost in the woods and came back empty-handed.",
                $"**{pet.Name}** was chased by a swarm of bees and had to retreat!",
                $"**{pet.Name}** fell into a river and barely made it back.",
                $"**{pet.Name}** found nothing but an empty cave.",
                $"**{pet.Name}** was scared off by a loud thunderstorm.",
                $"**{pet.Name}** tripped over a rock and limped back home.",
            };
            narrative = narratives[_rng.Next(0, narratives.Length)];
            xpEarned = 10;
            pet.Happiness = Math.Max(0, pet.Happiness - 5);
        }

        pet.Xp += xpEarned;

        var oldStage = pet.EvolutionStage;
        CheckLevelUp(pet);
        if (pet.EvolutionStage != oldStage)
        {
            var species = GetSpeciesData(pet.Species);
            if (species is not null)
                ApplyEvolutionVisuals(pet, species);
        }

        await SavePetAsync(pet);
        return (success, narrative, currencyEarned, xpEarned, pet, null);
    }

    public async Task<(bool won, string narrative, long currencyEarned, long xpEarned, int enemyLevel, string enemyName, Pet pet, string error)>
        BattleAsync(ulong userId, ulong guildId, int petId)
    {
        var pet = await GetPetAsync(userId, guildId, petId);
        if (pet is null)
            return (false, null, 0, 0, 0, null, null, "You don't have a pet with that ID!");

        if (pet.Energy < 20)
            return (false, null, 0, 0, 0, null, null, $"**{pet.Name}** needs at least 20 energy to battle!");

        pet.Energy = Math.Max(0, pet.Energy - 20);

        var wildCreatures = new[]
        {
            ("Wild Rat", 3, 4, 2),
            ("Forest Spider", 4, 6, 3),
            ("Cave Bat", 5, 7, 4),
            ("Rogue Goblin", 7, 5, 5),
            ("Stone Golem", 10, 2, 6),
            ("Shadow Wraith", 6, 8, 8),
            ("Fire Imp", 7, 7, 6),
            ("Ice Elemental", 8, 5, 9),
            ("Dire Wolf", 9, 8, 5),
            ("Thunder Drake", 10, 7, 8),
            ("Swamp Troll", 11, 4, 6),
            ("Dark Knight", 9, 6, 7),
            ("Ancient Lich", 7, 5, 12),
            ("Lava Serpent", 10, 9, 7),
            ("Storm Giant", 12, 5, 8),
        };

        var maxIndex = Math.Min(wildCreatures.Length, 3 + pet.Level / 3);
        var enemyIdx = _rng.Next(0, maxIndex);
        var (enemyName, eStr, eAgi, eInt) = wildCreatures[enemyIdx];
        var enemyLevel = 1 + (enemyIdx * 3) + _rng.Next(0, 3);

        var levelScale = 1.0 + (enemyLevel * 0.15);
        var enemyPower = (int)((eStr + eAgi + eInt) * levelScale);
        var petPower = pet.Strength + pet.Agility + pet.Intelligence + _rng.Next(0, pet.Level + 1);

        var won = petPower >= enemyPower || _rng.Next(0, 100) < 20;

        string narrative;
        long currencyEarned = 0;
        long xpEarned;

        if (won)
        {
            pet.BattlesWon++;
            var winNarratives = new[]
            {
                $"**{pet.Name}** lunged at the {enemyName} and won with a decisive blow!",
                $"**{pet.Name}** outsmarted the {enemyName} and claimed victory!",
                $"After a fierce clash, **{pet.Name}** stood tall over the defeated {enemyName}!",
                $"**{pet.Name}** dodged every attack and struck the {enemyName} down!",
                $"With a burst of energy, **{pet.Name}** overwhelmed the {enemyName}!",
            };
            narrative = winNarratives[_rng.Next(0, winNarratives.Length)];
            currencyEarned = 15 + (enemyLevel * 3) + _rng.Next(0, 31);
            xpEarned = 20 + (enemyLevel * 2) + _rng.Next(0, 11);

            await _cs.AddAsync(userId, currencyEarned, new("pet", "battle", $"{pet.Name} battle reward"));
            pet.Happiness = Math.Min(100, pet.Happiness + 8);
        }
        else
        {
            var loseNarratives = new[]
            {
                $"The {enemyName} was too powerful! **{pet.Name}** retreated in defeat.",
                $"**{pet.Name}** fought bravely but the {enemyName} proved too strong.",
                $"The {enemyName} landed a critical hit! **{pet.Name}** had to flee.",
                $"**{pet.Name}** was outmatched by the fierce {enemyName}.",
                $"Despite a valiant effort, **{pet.Name}** fell to the {enemyName}.",
            };
            narrative = loseNarratives[_rng.Next(0, loseNarratives.Length)];
            xpEarned = 8 + _rng.Next(0, 6);
            pet.Happiness = Math.Max(0, pet.Happiness - 8);
        }

        pet.Xp += xpEarned;

        var oldStage = pet.EvolutionStage;
        CheckLevelUp(pet);
        if (pet.EvolutionStage != oldStage)
        {
            var species = GetSpeciesData(pet.Species);
            if (species is not null)
                ApplyEvolutionVisuals(pet, species);
        }

        await SavePetAsync(pet);
        return (won, narrative, currencyEarned, xpEarned, enemyLevel, enemyName, pet, null);
    }
}
