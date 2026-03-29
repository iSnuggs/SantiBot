#nullable disable
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.TowerDefense;

public sealed class TowerDefenseService : INService
{
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, TDGame> ActiveGames = new();

    public TowerDefenseService(ICurrencyService cs)
    {
        _cs = cs;
    }

    public enum TowerType { Archer, Mage, Cannon }

    public class Tower
    {
        public TowerType Type { get; set; }
        public int Position { get; set; }
        public int Level { get; set; } = 1;
        public int Damage => Type switch
        {
            TowerType.Archer => 10 * Level,
            TowerType.Mage => 8 * Level, // AoE hits multiple
            TowerType.Cannon => 20 * Level,
            _ => 5
        };
        public int Cost => Type switch
        {
            TowerType.Archer => 100,
            TowerType.Mage => 200,
            TowerType.Cannon => 300,
            _ => 100
        };
    }

    public class TDGame
    {
        public ulong ChannelId { get; set; }
        public List<ulong> Players { get; set; } = new();
        public List<Tower> Towers { get; set; } = new();
        public int Wave { get; set; }
        public int BaseHp { get; set; } = 100;
        public int Gold { get; set; } = 500;
        public bool IsActive { get; set; } = true;
    }

    public (bool Success, string Message) StartGame(ulong channelId, ulong userId)
    {
        if (ActiveGames.ContainsKey(channelId))
            return (false, "A Tower Defense game is already running!");

        var game = new TDGame
        {
            ChannelId = channelId,
            Players = new() { userId }
        };

        ActiveGames[channelId] = game;
        return (true, "🏰 **Tower Defense Started!**\nGold: 500 | Base HP: 100\nPlace towers: `.td place <archer|mage|cannon> <1-10>`\nStart waves: `.td wave`\n\nTower costs: Archer 100g, Mage 200g, Cannon 300g");
    }

    public (bool Success, string Message) PlaceTower(ulong channelId, string typeName, int position)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game running!");

        if (position is < 1 or > 10)
            return (false, "Position must be 1-10!");

        if (!Enum.TryParse<TowerType>(typeName, true, out var type))
            return (false, "Tower types: Archer, Mage, Cannon");

        if (game.Towers.Any(t => t.Position == position))
            return (false, "Position already occupied!");

        var tower = new Tower { Type = type, Position = position };
        if (game.Gold < tower.Cost)
            return (false, $"Not enough gold! Need {tower.Cost}, have {game.Gold}.");

        game.Gold -= tower.Cost;
        game.Towers.Add(tower);

        return (true, $"🗼 **{type}** tower placed at position {position}! (Gold: {game.Gold})");
    }

    public (bool Success, string Message) UpgradeTower(ulong channelId, int position)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game running!");

        var tower = game.Towers.FirstOrDefault(t => t.Position == position);
        if (tower is null)
            return (false, "No tower at that position!");

        if (tower.Level >= 3)
            return (false, "Tower is already max level!");

        var cost = tower.Cost * tower.Level;
        if (game.Gold < cost)
            return (false, $"Need {cost} gold to upgrade!");

        game.Gold -= cost;
        tower.Level++;

        return (true, $"🔧 Tower at position {position} upgraded to level {tower.Level}! (Gold: {game.Gold})");
    }

    public async Task<(bool Success, string Message)> RunWaveAsync(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return (false, "No game running!");

        game.Wave++;
        var sb = new StringBuilder();
        sb.AppendLine($"🌊 **Wave {game.Wave}/20**\n");

        // Spawn enemies
        var enemyCount = 2 + game.Wave;
        var enemyHp = 20 + game.Wave * 10;
        var enemyDamage = 3 + game.Wave * 2;
        var totalEnemyHp = enemyCount * enemyHp;
        var enemiesKilled = 0;

        sb.AppendLine($"Enemies: {enemyCount} (HP: {enemyHp} each, ATK: {enemyDamage})");

        // Towers attack
        var totalTowerDamage = 0;
        foreach (var tower in game.Towers)
        {
            var hits = tower.Type == TowerType.Mage ? Math.Min(3, enemyCount) : 1; // Mage hits up to 3
            var totalDmg = tower.Damage * hits;
            totalTowerDamage += totalDmg;
            sb.AppendLine($"  🗼 {tower.Type} (Lv{tower.Level}) at pos {tower.Position}: {totalDmg} damage");
        }

        // Calculate remaining enemies
        var hpDamaged = totalTowerDamage;
        while (hpDamaged >= enemyHp && enemiesKilled < enemyCount)
        {
            hpDamaged -= enemyHp;
            enemiesKilled++;
        }

        var surviving = enemyCount - enemiesKilled;
        var baseDamage = surviving * enemyDamage;
        game.BaseHp -= baseDamage;

        sb.AppendLine($"\nEnemies killed: {enemiesKilled}/{enemyCount}");
        if (surviving > 0)
            sb.AppendLine($"💥 {surviving} enemies reached the base! -{baseDamage} HP");

        sb.AppendLine($"Base HP: {game.BaseHp}/100");

        // Wave rewards
        var goldReward = 50 + game.Wave * 20;
        game.Gold += goldReward;
        sb.AppendLine($"Gold earned: +{goldReward} (Total: {game.Gold})");

        if (game.BaseHp <= 0)
        {
            ActiveGames.TryRemove(channelId, out _);
            sb.AppendLine($"\n💀 **Base destroyed on Wave {game.Wave}!** Game Over!");
            return (true, sb.ToString());
        }

        if (game.Wave >= 20)
        {
            var reward = 2000L;
            foreach (var p in game.Players)
                await _cs.AddAsync(p, reward, new TxData("td", "win"));

            ActiveGames.TryRemove(channelId, out _);
            sb.AppendLine($"\n🏆 **All 20 waves survived!** Each player earns {reward} 🥠!");
            return (true, sb.ToString());
        }

        return (true, sb.ToString());
    }

    public string GetStatus(ulong channelId)
    {
        if (!ActiveGames.TryGetValue(channelId, out var game))
            return null;

        var sb = new StringBuilder();
        sb.AppendLine($"Wave: {game.Wave}/20 | Base HP: {game.BaseHp}/100 | Gold: {game.Gold}");
        sb.AppendLine("Towers:");
        foreach (var t in game.Towers.OrderBy(t => t.Position))
            sb.AppendLine($"  Pos {t.Position}: {t.Type} Lv{t.Level} (DMG: {t.Damage})");

        return sb.ToString();
    }
}
