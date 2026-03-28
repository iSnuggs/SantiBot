using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Gambling.Pets;

public sealed class PetService(ICurrencyService _cur, DbService _db) : INService
{
    private static readonly HashSet<string> _validSpecies =
        ["Cat", "Dog", "Bunny", "Fox", "Dragon", "Phoenix"];

    private const long FEED_COST = 50;
    private const int FEED_HUNGER_RESTORE = 30;
    private const int PLAY_HAPPINESS_RESTORE = 25;
    private static readonly TimeSpan PLAY_COOLDOWN = TimeSpan.FromHours(1);

    // Hunger drops ~10 per hour, happiness ~5 per hour
    private const double HUNGER_DECAY_PER_HOUR = 10.0;
    private const double HAPPINESS_DECAY_PER_HOUR = 5.0;

    public static int XpForLevel(int level) => level * 100;

    public bool IsValidSpecies(string species) => _validSpecies.Contains(species);

    public static IReadOnlySet<string> ValidSpecies => _validSpecies;

    private static (int hunger, int happiness) ApplyDecay(UserPet pet)
    {
        var hoursSinceFed = (DateTime.UtcNow - pet.LastFedUtc).TotalHours;
        var hoursSincePlayed = (DateTime.UtcNow - pet.LastPlayedUtc).TotalHours;

        var hunger = Math.Max(0, pet.Hunger - (int)(hoursSinceFed * HUNGER_DECAY_PER_HOUR));
        var happiness = Math.Max(0, pet.Happiness - (int)(hoursSincePlayed * HAPPINESS_DECAY_PER_HOUR));

        return (hunger, happiness);
    }

    public async Task<(bool success, string? error)> AdoptAsync(ulong userId, string species, string name)
    {
        if (!IsValidSpecies(species))
            return (false, "invalid_species");

        await using var ctx = _db.GetDbContext();

        var existing = await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (existing is not null)
            return (false, "already_have_pet");

        var now = DateTime.UtcNow;
        await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .InsertAsync(() => new UserPet
            {
                UserId = userId,
                Name = name,
                Species = species,
                Level = 1,
                Xp = 0,
                Hunger = 100,
                Happiness = 100,
                LastFedUtc = now,
                LastPlayedUtc = now,
                TotalEarned = 0
            });

        return (true, null);
    }

    public async Task<(UserPet? pet, int effectiveHunger, int effectiveHappiness)> GetPetAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (pet is null)
            return (null, 0, 0);

        var (hunger, happiness) = ApplyDecay(pet);
        return (pet, hunger, happiness);
    }

    public async Task<(bool success, string? error)> FeedPetAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (pet is null)
            return (false, "no_pet");

        if (!await _cur.RemoveAsync(userId, FEED_COST, new("pet", "feed")))
            return (false, "not_enough");

        var (currentHunger, _) = ApplyDecay(pet);
        var newHunger = Math.Min(100, currentHunger + FEED_HUNGER_RESTORE);
        var now = DateTime.UtcNow;

        // Feeding gives XP
        var newXp = pet.Xp + 10;
        var newLevel = pet.Level;
        while (newXp >= XpForLevel(newLevel))
        {
            newXp -= XpForLevel(newLevel);
            newLevel++;
        }

        await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .UpdateAsync(_ => new UserPet
            {
                Hunger = newHunger,
                LastFedUtc = now,
                Xp = newXp,
                Level = newLevel
            });

        return (true, null);
    }

    public async Task<(bool success, string? error, TimeSpan? cooldownLeft)> PlayWithPetAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var pet = await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (pet is null)
            return (false, "no_pet", null);

        var elapsed = DateTime.UtcNow - pet.LastPlayedUtc;
        if (elapsed < PLAY_COOLDOWN)
            return (false, "cooldown", PLAY_COOLDOWN - elapsed);

        var (_, currentHappiness) = ApplyDecay(pet);
        var newHappiness = Math.Min(100, currentHappiness + PLAY_HAPPINESS_RESTORE);
        var now = DateTime.UtcNow;

        // Playing gives XP
        var newXp = pet.Xp + 15;
        var newLevel = pet.Level;
        while (newXp >= XpForLevel(newLevel))
        {
            newXp -= XpForLevel(newLevel);
            newLevel++;
        }

        await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .UpdateAsync(_ => new UserPet
            {
                Happiness = newHappiness,
                LastPlayedUtc = now,
                Xp = newXp,
                Level = newLevel
            });

        return (true, null, null);
    }

    public async Task<(bool success, string? error)> RenamePetAsync(ulong userId, string newName)
    {
        await using var ctx = _db.GetDbContext();

        var rows = await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .Where(x => x.UserId == userId)
            .UpdateAsync(_ => new UserPet { Name = newName });

        return rows > 0 ? (true, null) : (false, "no_pet");
    }

    public async Task<List<UserPet>> GetLeaderboardAsync(int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.Set<UserPet>()
            .ToLinqToDBTable()
            .OrderByDescending(x => x.Level)
            .ThenByDescending(x => x.Xp)
            .Take(count)
            .ToListAsyncLinqToDB();
    }
}
