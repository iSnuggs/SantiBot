#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;


namespace SantiBot.Modules.Games.Pokemon;

public sealed class PokemonService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    public readonly ConcurrentDictionary<ulong, PokeBattle> ActiveBattles = new();

    // Cooldown tracking
    private readonly ConcurrentDictionary<ulong, DateTime> _catchCooldowns = new();
    private readonly ConcurrentDictionary<ulong, DateTime> _trainCooldowns = new();
    private const int CATCH_COOLDOWN_SECONDS = 30;
    private const int TRAIN_COOLDOWN_SECONDS = 60;

    // Type -> (name, baseHp, baseAtk, baseDef)
    public static readonly (string Name, string Type, int BaseHp, int BaseAtk, int BaseDef)[] AllCreatures =
    [
        ("Flamepup", "Fire", 45, 52, 43),
        ("Blazewolf", "Fire", 65, 72, 53),
        ("Infernox", "Fire", 85, 92, 63),
        ("Aquafish", "Water", 50, 48, 55),
        ("Tideshark", "Water", 70, 68, 75),
        ("Tsunamix", "Water", 90, 88, 85),
        ("Leafkit", "Grass", 48, 45, 50),
        ("Vinebear", "Grass", 68, 65, 70),
        ("Thornking", "Grass", 88, 85, 90),
        ("Zaprat", "Electric", 42, 55, 40),
        ("Voltfox", "Electric", 62, 75, 60),
        ("Thunderlord", "Electric", 82, 95, 70),
        ("Emberfox", "Fire", 55, 60, 45),
        ("Bubbletoad", "Water", 60, 50, 65),
        ("Mossturtle", "Grass", 58, 48, 68),
        ("Sparkbird", "Electric", 52, 58, 42),
        ("Pyrodrake", "Fire", 75, 80, 55),
        ("Coralwhale", "Water", 80, 60, 85),
        ("Petalfae", "Grass", 55, 70, 55),
        ("Boltlion", "Electric", 72, 82, 52),
        ("Scorchsaur", "Fire", 60, 65, 50),
        ("Dewdrop", "Water", 45, 42, 48),
        ("Fernsnake", "Grass", 50, 55, 45),
        ("Staticeel", "Electric", 55, 62, 50),
        ("Magmabear", "Fire", 80, 75, 70),
        ("Iceswan", "Water", 65, 55, 70),
        ("Bamboolm", "Grass", 70, 60, 75),
        ("Plasmawing", "Electric", 60, 70, 55),
        ("Cinders", "Fire", 40, 48, 38),
        ("Ripplefin", "Water", 42, 40, 50),
        ("Sproutling", "Grass", 44, 42, 46),
        ("Joltmouse", "Electric", 38, 50, 35),
        ("Volcabug", "Fire", 50, 58, 42),
        ("Shellduck", "Water", 55, 45, 60),
        ("Rootox", "Grass", 65, 55, 72),
        ("Zapgecko", "Electric", 48, 60, 40),
        ("Blazeraptor", "Fire", 70, 85, 55),
        ("Oceanlurk", "Water", 75, 65, 80),
        ("Jungleclaw", "Grass", 72, 70, 68),
        ("Shockpanther", "Electric", 68, 85, 55),
        ("Embermoth", "Fire", 52, 55, 48),
        ("Mistfrog", "Water", 58, 52, 62),
        ("Cactusfist", "Grass", 62, 68, 58),
        ("Ampweasel", "Electric", 50, 65, 45),
        ("Phoenixchick", "Fire", 48, 50, 42),
        ("Clamsage", "Water", 52, 48, 58),
        ("Dandelynx", "Grass", 55, 52, 55),
        ("Circuitbug", "Electric", 45, 55, 42),
        ("Ashdragon", "Fire", 95, 100, 70),
        ("Leviathin", "Water", 100, 90, 95),
    ];

    private static readonly Dictionary<string, Dictionary<string, double>> TypeChart = new()
    {
        ["Fire"] = new() { ["Fire"] = 0.5, ["Water"] = 0.5, ["Grass"] = 2.0, ["Electric"] = 1.0 },
        ["Water"] = new() { ["Fire"] = 2.0, ["Water"] = 0.5, ["Grass"] = 0.5, ["Electric"] = 0.5 },
        ["Grass"] = new() { ["Fire"] = 0.5, ["Water"] = 2.0, ["Grass"] = 0.5, ["Electric"] = 1.0 },
        ["Electric"] = new() { ["Fire"] = 1.0, ["Water"] = 2.0, ["Grass"] = 0.5, ["Electric"] = 0.5 },
    };

    public PokemonService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public class PokeBattle
    {
        public ulong ChannelId { get; set; }
        public ulong Player1Id { get; set; }
        public ulong Player2Id { get; set; }
        public UserPokemon Pokemon1 { get; set; }
        public UserPokemon Pokemon2 { get; set; }
        public int P1CurrentHp { get; set; }
        public int P2CurrentHp { get; set; }
        public bool IsPlayer1Turn { get; set; } = true;
    }

    public async Task<(bool Success, string Message, UserPokemon Caught)> CatchAsync(ulong userId)
    {
        // Cooldown check
        if (_catchCooldowns.TryGetValue(userId, out var lastCatch))
        {
            var cooldownEnd = lastCatch.AddSeconds(CATCH_COOLDOWN_SECONDS);
            if (DateTime.UtcNow < cooldownEnd)
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                return (false, $"You're still searching! Try again in **{remaining.Seconds}s**.", null);
            }
        }
        _catchCooldowns[userId] = DateTime.UtcNow;

        // 70% catch rate
        if (_rng.Next(100) >= 70)
            return (false, "The creature escaped! Try again.", null);

        var template = AllCreatures[_rng.Next(AllCreatures.Length)];
        var level = _rng.Next(1, 11);
        var pokemon = new UserPokemon
        {
            UserId = userId,
            Name = template.Name,
            Type = template.Type,
            Level = level,
            MaxHp = template.BaseHp + level * 3,
            Hp = template.BaseHp + level * 3,
            Attack = template.BaseAtk + level * 2,
            Defense = template.BaseDef + level * 2,
            Xp = 0,
            XpToNext = level * 50
        };

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<UserPokemon>().InsertAsync(() => new UserPokemon
        {
            UserId = userId,
            Name = pokemon.Name,
            Type = pokemon.Type,
            Level = pokemon.Level,
            MaxHp = pokemon.MaxHp,
            Hp = pokemon.Hp,
            Attack = pokemon.Attack,
            Defense = pokemon.Defense,
            Xp = pokemon.Xp,
            XpToNext = pokemon.XpToNext,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"You caught a **{pokemon.Name}**! ({pokemon.Type} type, Level {pokemon.Level})", pokemon);
    }

    public async Task<List<UserPokemon>> GetPokemonListAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserPokemon>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Level)
            .ToListAsyncLinqToDB();
    }

    public async Task<UserPokemon> GetPokemonInfoAsync(ulong userId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserPokemon>()
            .FirstOrDefaultAsyncLinqToDB(p => p.UserId == userId && p.Name == name);
    }

    public async Task<(bool Success, string Message)> TrainAsync(ulong userId, string pokemonName)
    {
        // Cooldown check
        if (_trainCooldowns.TryGetValue(userId, out var lastTrain))
        {
            var cooldownEnd = lastTrain.AddSeconds(TRAIN_COOLDOWN_SECONDS);
            if (DateTime.UtcNow < cooldownEnd)
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                return (false, $"Your creature is resting! Try again in **{remaining.Seconds}s**.");
            }
        }
        _trainCooldowns[userId] = DateTime.UtcNow;

        await using var ctx = _db.GetDbContext();
        var pokemon = await ctx.GetTable<UserPokemon>()
            .FirstOrDefaultAsyncLinqToDB(p => p.UserId == userId && p.Name == pokemonName);

        if (pokemon is null)
            return (false, "You don't have that creature!");

        var xpGain = _rng.Next(15, 35);
        var newXp = pokemon.Xp + xpGain;

        if (newXp >= pokemon.XpToNext)
        {
            var newLevel = pokemon.Level + 1;
            await ctx.GetTable<UserPokemon>()
                .Where(p => p.Id == pokemon.Id)
                .UpdateAsync(p => new UserPokemon
                {
                    Level = newLevel,
                    Xp = 0,
                    XpToNext = newLevel * 50,
                    MaxHp = p.MaxHp + 3,
                    Hp = p.MaxHp + 3,
                    Attack = p.Attack + 2,
                    Defense = p.Defense + 2
                });

            return (true, $"**{pokemon.Name}** gained {xpGain} XP and leveled up to **Level {newLevel}**!");
        }

        await ctx.GetTable<UserPokemon>()
            .Where(p => p.Id == pokemon.Id)
            .UpdateAsync(p => new UserPokemon { Xp = newXp });

        return (true, $"**{pokemon.Name}** gained {xpGain} XP! ({newXp}/{pokemon.XpToNext} to next level)");
    }

    public async Task<(bool Success, string Message)> StartBattleAsync(ulong channelId, ulong challengerId, ulong opponentId)
    {
        if (challengerId == opponentId)
            return (false, "You can't battle yourself!");

        if (ActiveBattles.ContainsKey(channelId))
            return (false, "A battle is already happening here!");

        await using var ctx = _db.GetDbContext();
        var p1Pokemon = await ctx.GetTable<UserPokemon>()
            .Where(p => p.UserId == challengerId)
            .OrderByDescending(p => p.Level)
            .FirstOrDefaultAsyncLinqToDB();

        var p2Pokemon = await ctx.GetTable<UserPokemon>()
            .Where(p => p.UserId == opponentId)
            .OrderByDescending(p => p.Level)
            .FirstOrDefaultAsyncLinqToDB();

        if (p1Pokemon is null)
            return (false, "You don't have any creatures! Use `.catch` first.");
        if (p2Pokemon is null)
            return (false, "Your opponent doesn't have any creatures!");

        var battle = new PokeBattle
        {
            ChannelId = channelId,
            Player1Id = challengerId,
            Player2Id = opponentId,
            Pokemon1 = p1Pokemon,
            Pokemon2 = p2Pokemon,
            P1CurrentHp = p1Pokemon.MaxHp,
            P2CurrentHp = p2Pokemon.MaxHp,
        };

        ActiveBattles[channelId] = battle;

        return (true, $"⚔️ **Battle!** <@{challengerId}>'s **{p1Pokemon.Name}** (Lv{p1Pokemon.Level} {p1Pokemon.Type}) vs <@{opponentId}>'s **{p2Pokemon.Name}** (Lv{p2Pokemon.Level} {p2Pokemon.Type})\n<@{challengerId}>'s turn! Use `.battle attack`.");
    }

    public async Task<(bool Success, string Message)> AttackAsync(ulong channelId, ulong attackerId)
    {
        if (!ActiveBattles.TryGetValue(channelId, out var battle))
            return (false, "No active battle!");

        var isP1 = attackerId == battle.Player1Id;
        if (isP1 != battle.IsPlayer1Turn)
            return (false, "Not your turn!");

        var attacker = isP1 ? battle.Pokemon1 : battle.Pokemon2;
        var defender = isP1 ? battle.Pokemon2 : battle.Pokemon1;

        var effectiveness = TypeChart.GetValueOrDefault(attacker.Type)?.GetValueOrDefault(defender.Type, 1.0) ?? 1.0;
        var baseDamage = Math.Max(1, attacker.Attack - defender.Defense / 2);
        var damage = (int)(baseDamage * effectiveness * (0.85 + _rng.NextDouble() * 0.3));
        damage = Math.Max(1, damage);

        var effectText = effectiveness switch
        {
            >= 2.0 => "It's super effective!",
            <= 0.5 => "It's not very effective...",
            _ => ""
        };

        if (isP1)
            battle.P2CurrentHp -= damage;
        else
            battle.P1CurrentHp -= damage;

        var result = $"**{attacker.Name}** attacks **{defender.Name}** for **{damage}** damage! {effectText}";

        // Check if battle ended
        if ((isP1 && battle.P2CurrentHp <= 0) || (!isP1 && battle.P1CurrentHp <= 0))
        {
            var winnerId = isP1 ? battle.Player1Id : battle.Player2Id;
            var winnerPokemon = isP1 ? battle.Pokemon1 : battle.Pokemon2;
            await _cs.AddAsync(winnerId, 100, new TxData("pokemon", "battle-win"));
            ActiveBattles.TryRemove(channelId, out _);

            return (true, $"{result}\n\n🏆 **{winnerPokemon.Name}** wins! <@{winnerId}> earns 100 🥠!");
        }

        battle.IsPlayer1Turn = !battle.IsPlayer1Turn;
        var nextAttacker = battle.IsPlayer1Turn ? battle.Player1Id : battle.Player2Id;

        return (true, $"{result}\nHP: {battle.Pokemon1.Name} {battle.P1CurrentHp}/{battle.Pokemon1.MaxHp} | {battle.Pokemon2.Name} {battle.P2CurrentHp}/{battle.Pokemon2.MaxHp}\n<@{nextAttacker}>'s turn!");
    }
}
