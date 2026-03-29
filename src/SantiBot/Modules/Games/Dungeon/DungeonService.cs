#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.Dungeon;

public sealed class DungeonService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, DungeonRun> ActiveDungeons = new();

    public DungeonService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public class DungeonRun
    {
        public ulong ChannelId { get; set; }
        public List<(ulong UserId, string Username, int Hp, int MaxHp, int Attack)> Party { get; set; } = new();
        public int Difficulty { get; set; } // 1-5
        public int CurrentRoom { get; set; }
        public int MaxRooms { get; set; }
        public long TotalLoot { get; set; }
        public string CurrentMonster { get; set; }
        public int MonsterHp { get; set; }
        public int MonsterAtk { get; set; }
        public List<string> Inventory { get; set; } = new();
    }

    private static readonly (string Name, int BaseHp, int BaseAtk)[] Monsters =
    [
        ("Goblin", 30, 8),
        ("Skeleton", 40, 12),
        ("Dark Mage", 35, 18),
        ("Orc Warrior", 60, 15),
        ("Giant Spider", 45, 14),
        ("Undead Knight", 70, 20),
        ("Fire Drake", 80, 25),
        ("Shadow Demon", 100, 30),
        ("Lich King", 120, 35),
        ("Ancient Dragon", 150, 40),
    ];

    public (bool Success, string Message) EnterDungeon(ulong channelId, ulong userId, string username, int difficulty)
    {
        if (ActiveDungeons.ContainsKey(channelId))
            return (false, "A dungeon run is already in progress!");

        difficulty = Math.Clamp(difficulty, 1, 5);
        var maxHp = 100 + difficulty * 20;

        var run = new DungeonRun
        {
            ChannelId = channelId,
            Difficulty = difficulty,
            CurrentRoom = 0,
            MaxRooms = 5 + difficulty * 2,
            Party = new() { (userId, username, maxHp, maxHp, 20 + difficulty * 5) }
        };

        ActiveDungeons[channelId] = run;
        return (true, $"⚔️ **Dungeon Run** (Difficulty {difficulty})\n{username} enters the dungeon! ({run.MaxRooms} rooms)\nInvite others with `.dungeon party invite @user` or `.dungeon explore` to begin!");
    }

    public (bool Success, string Message) InviteToParty(ulong channelId, ulong userId, string username)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");

        if (run.Party.Count >= 4)
            return (false, "Party is full (max 4)!");

        if (run.Party.Any(p => p.UserId == userId))
            return (false, "Already in the party!");

        var maxHp = 100 + run.Difficulty * 20;
        run.Party.Add((userId, username, maxHp, maxHp, 20 + run.Difficulty * 5));
        return (true, $"{username} joins the party! ({run.Party.Count}/4)");
    }

    public async Task<(bool Success, string Message)> ExploreAsync(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");

        run.CurrentRoom++;
        var sb = new StringBuilder();
        sb.AppendLine($"🚪 **Room {run.CurrentRoom}/{run.MaxRooms}**\n");

        var eventType = _rng.Next(10);

        if (eventType < 6) // 60% monster encounter
        {
            var monsterIdx = Math.Min(_rng.Next(Monsters.Length), run.Difficulty * 2);
            var (name, baseHp, baseAtk) = Monsters[monsterIdx];
            run.CurrentMonster = name;
            run.MonsterHp = baseHp * run.Difficulty;
            run.MonsterAtk = baseAtk * run.Difficulty;

            sb.AppendLine($"👹 A **{name}** appears! (HP: {run.MonsterHp}, ATK: {run.MonsterAtk})");
            sb.AppendLine("Use `.dungeon attack` to fight or `.dungeon flee` to run!");
        }
        else if (eventType < 8) // 20% treasure
        {
            var loot = _rng.Next(50, 200) * run.Difficulty;
            run.TotalLoot += loot;
            sb.AppendLine($"💰 **Treasure found!** +{loot} 🥠 (Total: {run.TotalLoot})");

            if (run.CurrentRoom >= run.MaxRooms)
            {
                return await FinishDungeonAsync(channelId, run, sb);
            }
            sb.AppendLine("Continue with `.dungeon explore`!");
        }
        else if (eventType < 9) // 10% trap
        {
            var damage = _rng.Next(10, 30) * run.Difficulty;
            sb.AppendLine($"💥 **Trap!** Party takes {damage} damage!");

            for (int i = 0; i < run.Party.Count; i++)
            {
                var p = run.Party[i];
                var newHp = Math.Max(0, p.Hp - damage);
                run.Party[i] = (p.UserId, p.Username, newHp, p.MaxHp, p.Attack);
            }

            run.Party.RemoveAll(p => p.Hp <= 0);
            if (run.Party.Count == 0)
            {
                ActiveDungeons.TryRemove(channelId, out _);
                sb.AppendLine("💀 **Party wiped!** Dungeon failed.");
                return (true, sb.ToString());
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);

            sb.AppendLine("Continue with `.dungeon explore`!");
        }
        else // 10% healing
        {
            var heal = 30 * run.Difficulty;
            sb.AppendLine($"✨ **Healing Spring!** Party restores {heal} HP!");

            for (int i = 0; i < run.Party.Count; i++)
            {
                var p = run.Party[i];
                var newHp = Math.Min(p.MaxHp, p.Hp + heal);
                run.Party[i] = (p.UserId, p.Username, newHp, p.MaxHp, p.Attack);
            }

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);

            sb.AppendLine("Continue with `.dungeon explore`!");
        }

        return (true, sb.ToString());
    }

    public async Task<(bool Success, string Message)> AttackAsync(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");

        if (run.CurrentMonster is null)
            return (false, "No monster to fight!");

        var sb = new StringBuilder();

        // Party attacks
        foreach (var p in run.Party)
        {
            var damage = Math.Max(1, p.Attack + _rng.Next(-5, 10));
            run.MonsterHp -= damage;
            sb.AppendLine($"⚔️ {p.Username} deals {damage} damage!");
        }

        if (run.MonsterHp <= 0)
        {
            var loot = _rng.Next(30, 100) * run.Difficulty;
            run.TotalLoot += loot;
            run.CurrentMonster = null;
            sb.AppendLine($"\n💀 **{run.CurrentMonster ?? "Monster"} defeated!** +{loot} 🥠");

            if (run.CurrentRoom >= run.MaxRooms)
                return await FinishDungeonAsync(channelId, run, sb);

            sb.AppendLine("Use `.dungeon explore` for the next room!");
            return (true, sb.ToString());
        }

        // Monster counterattacks
        sb.AppendLine($"\n👹 Monster HP: {run.MonsterHp}");
        var targetIdx = _rng.Next(run.Party.Count);
        var target = run.Party[targetIdx];
        var monsterDmg = Math.Max(1, run.MonsterAtk + _rng.Next(-3, 5));
        var newTargetHp = Math.Max(0, target.Hp - monsterDmg);
        run.Party[targetIdx] = (target.UserId, target.Username, newTargetHp, target.MaxHp, target.Attack);

        sb.AppendLine($"👹 Monster attacks {target.Username} for {monsterDmg}! ({newTargetHp} HP left)");

        if (newTargetHp <= 0)
        {
            sb.AppendLine($"💀 {target.Username} has fallen!");
            run.Party.RemoveAt(targetIdx);
        }

        if (run.Party.Count == 0)
        {
            ActiveDungeons.TryRemove(channelId, out _);
            sb.AppendLine("\n💀 **Party wiped!** Dungeon failed.");
        }

        return (true, sb.ToString());
    }

    public (bool Success, string Message) FleeAsync(ulong channelId)
    {
        if (!ActiveDungeons.TryGetValue(channelId, out var run))
            return (false, "No dungeon run!");

        if (_rng.Next(100) < 60) // 60% flee success
        {
            run.CurrentMonster = null;
            return (true, "🏃 Party fled successfully! Use `.dungeon explore` to continue.");
        }

        // Failed flee — monster gets free attack
        if (run.Party.Count > 0)
        {
            var targetIdx = _rng.Next(run.Party.Count);
            var target = run.Party[targetIdx];
            var damage = run.MonsterAtk;
            var newHp = Math.Max(0, target.Hp - damage);
            run.Party[targetIdx] = (target.UserId, target.Username, newHp, target.MaxHp, target.Attack);

            if (newHp <= 0)
                run.Party.RemoveAt(targetIdx);

            if (run.Party.Count == 0)
            {
                ActiveDungeons.TryRemove(channelId, out _);
                return (true, $"🏃 Flee failed! Monster attacks {target.Username} for {damage}! 💀 Party wiped!");
            }

            return (false, $"Flee failed! Monster attacks {target.Username} for {damage}! Try again or fight!");
        }

        return (false, "Flee failed!");
    }

    private async Task<(bool Success, string Message)> FinishDungeonAsync(ulong channelId, DungeonRun run, StringBuilder sb)
    {
        var bonusLoot = run.Difficulty * 200;
        run.TotalLoot += bonusLoot;
        var perPlayer = run.TotalLoot / run.Party.Count;

        sb.AppendLine($"\n🏆 **Dungeon Complete!** Bonus: {bonusLoot} 🥠");
        sb.AppendLine($"Total Loot: {run.TotalLoot} 🥠 ({perPlayer} 🥠 each)");

        foreach (var p in run.Party)
            await _cs.AddAsync(p.UserId, perPlayer, new TxData("dungeon", "loot"));

        ActiveDungeons.TryRemove(channelId, out _);
        return (true, sb.ToString());
    }
}
