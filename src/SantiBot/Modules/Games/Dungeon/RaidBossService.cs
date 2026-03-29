#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;
using System.Text;

namespace SantiBot.Modules.Games.Dungeon;

public sealed class RaidBossService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();

    // Cooldown per user per guild (45 seconds)
    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), DateTime> _attackCooldowns = new();
    private static readonly TimeSpan AttackCooldown = TimeSpan.FromSeconds(45);

    public RaidBossService(DbService db, ICurrencyService cs, DungeonService dungeonService)
    {
        _db = db;
        _cs = cs;

        // Wire up the random spawn callback so dungeon completions can trigger raid bosses
        dungeonService.OnDungeonClearedAsync = TryRandomSpawnAsync;
    }

    // ═══════════════════════════════════════════════════════════
    //  RAID BOSS TEMPLATES
    // ═══════════════════════════════════════════════════════════

    public static readonly (string Name, string Emoji, int BaseHp, int BaseAtk, int BaseDef, long BaseXp, long BaseCurrency, string Desc)[] BossTemplates =
    [
        ("Infernal Dragon",   "🐉", 10000, 60, 35, 500,  2000, "A dragon wreathed in hellfire — its breath melts steel"),
        ("The Lich Emperor",  "👑", 8000,  80, 25, 600,  2500, "An ancient king who conquered death itself"),
        ("Kraken of the Deep","🦑", 12000, 50, 40, 550,  2200, "Tentacles the size of towers emerge from the abyss"),
        ("World Eater",       "🌑", 15000, 70, 30, 800,  3000, "A void entity that devours entire realms"),
        ("Titan Golem",       "🗿", 20000, 40, 50, 700,  2800, "An ancient construct of stone and fury"),
        ("Phoenix Overlord",  "🔥", 9000,  90, 20, 650,  2400, "Reborn in flame — each phase it grows stronger"),
        ("Shadow Hydra",      "🐍", 11000, 65, 30, 600,  2300, "Cut one head, two more take its place"),
        ("Demon Lord Azaroth","😈", 18000, 85, 35, 900,  3500, "The lord of the ninth circle — bringer of despair"),
        ("Frost Wyrm",        "❄️", 13000, 55, 45, 700,  2600, "Its frozen breath can freeze time itself"),
        ("Abyssal Behemoth",  "👾", 25000, 75, 40, 1000, 4000, "From the deepest dungeon, something ancient stirs"),
    ];

    // Phase names and multipliers
    private static readonly (string Name, double AtkMult, double DefMult)[] Phases =
    [
        ("Awakened",    1.0,  1.0),  // Phase 1: 100-76%
        ("Enraged",     1.3,  0.9),  // Phase 2: 75-51%
        ("Berserk",     1.6,  0.8),  // Phase 3: 50-26%
        ("Death Throes", 2.0, 0.6),  // Phase 4: 25-0%
    ];

    // ═══════════════════════════════════════════════════════════
    //  SPAWN BOSS
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> SpawnBossAsync(ulong guildId, ulong channelId, string bossName = null, bool isRandom = false)
    {
        // Check if there's already an active raid
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<RaidBoss>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);

        if (existing is not null)
            return (false, $"A raid boss is already active: **{existing.Emoji} {existing.Name}** ({existing.CurrentHp}/{existing.MaxHp} HP)!\nDefeat it first!");

        // Pick a boss
        (string Name, string Emoji, int BaseHp, int BaseAtk, int BaseDef, long BaseXp, long BaseCurrency, string Desc) template;
        if (!string.IsNullOrWhiteSpace(bossName))
        {
            var match = BossTemplates.FirstOrDefault(b =>
                b.Name.Contains(bossName, StringComparison.OrdinalIgnoreCase));
            if (match.Name is null)
                return (false, $"Unknown boss! Available: {string.Join(", ", BossTemplates.Select(b => $"**{b.Name}**"))}");
            template = match;
        }
        else
        {
            template = BossTemplates[_rng.Next(BossTemplates.Length)];
        }

        // Scale HP based on server member count (we'll use a base multiplier)
        var boss = new RaidBoss
        {
            GuildId = guildId,
            ChannelId = channelId,
            Name = template.Name,
            Emoji = template.Emoji,
            MaxHp = template.BaseHp,
            CurrentHp = template.BaseHp,
            Attack = template.BaseAtk,
            Defense = template.BaseDef,
            XpReward = template.BaseXp,
            CurrencyReward = template.BaseCurrency,
            WasRandomSpawn = isRandom,
        };

        ctx.Add(boss);
        await ctx.SaveChangesAsync();

        var sb = new StringBuilder();
        sb.AppendLine($"# {template.Emoji} RAID BOSS SPAWNED!");
        sb.AppendLine($"## **{template.Name}**");
        sb.AppendLine($"*{template.Desc}*\n");
        sb.AppendLine($"HP: **{boss.MaxHp:N0}** | ATK: **{boss.Attack}** | DEF: **{boss.Defense}**");
        sb.AppendLine($"Rewards: **{boss.XpReward}** XP + **{boss.CurrencyReward}** currency");
        sb.AppendLine();
        if (isRandom)
            sb.AppendLine("*This boss appeared from the dungeons below!*");
        sb.AppendLine("Use `.raidboss attack` to fight! (45s cooldown per player)");
        sb.AppendLine("Use `.raidboss status` to see the fight!");

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  ATTACK BOSS
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> AttackBossAsync(ulong guildId, ulong userId, string username)
    {
        // Check cooldown
        var cooldownKey = (guildId, userId);
        if (_attackCooldowns.TryGetValue(cooldownKey, out var lastAttack))
        {
            var remaining = AttackCooldown - (DateTime.UtcNow - lastAttack);
            if (remaining > TimeSpan.Zero)
                return (false, $"You're on cooldown! Wait **{remaining.Seconds}s** before attacking again.");
        }

        await using var ctx = _db.GetDbContext();
        var boss = await ctx.GetTable<RaidBoss>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);

        if (boss is null)
            return (false, "No active raid boss! An admin can spawn one with `.raidboss spawn`.");

        // Get player stats
        var player = await ctx.GetTable<DungeonPlayer>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (player is null)
            return (false, "You need a dungeon character first! Use `.dungeon enter` to create one.");

        var equipped = await ctx.GetTable<DungeonItem>()
            .Where(x => x.UserId == userId && x.GuildId == guildId && x.IsEquipped)
            .ToListAsyncLinqToDB();

        var (maxHp, atk, def) = DungeonService.GetEffectiveStats(player, equipped);

        // Calculate phase
        var hpPercent = (double)boss.CurrentHp / boss.MaxHp * 100;
        var phaseIdx = hpPercent switch
        {
            > 75 => 0,
            > 50 => 1,
            > 25 => 2,
            _    => 3,
        };
        var phase = Phases[phaseIdx];

        // Player deals damage
        var baseDmg = Math.Max(1, atk - (int)(boss.Defense * phase.DefMult) / 3);
        var damage = baseDmg + _rng.Next(-5, 15);

        // Class bonuses vs raid boss
        var classEmoji = DungeonService.Classes.TryGetValue(player.Class, out var cls) ? cls.Emoji : "⚔️";
        var bonusText = "";

        switch (player.Class)
        {
            case "Warrior":
                damage = (int)(damage * 1.2);
                bonusText = " *(Heavy Strike)*";
                break;
            case "Mage" when _rng.Next(100) < 25:
                damage = (int)(damage * 1.8);
                bonusText = " *(Fireball!)*";
                break;
            case "Rogue" when _rng.Next(100) < 20:
                damage = (int)(damage * 2.0);
                bonusText = " *(Critical Strike!)*";
                break;
            case "Barbarian":
                damage = (int)(damage * 1.4);
                bonusText = " *(Rage!)*";
                break;
            case "Paladin" when boss.Name.Contains("Lich") || boss.Name.Contains("Demon"):
                damage = (int)(damage * 1.5);
                bonusText = " *(Holy Smite!)*";
                break;
            case "Necromancer":
                damage = (int)(damage * 1.1);
                bonusText = " *(Life Drain)*";
                break;
            case "Ranger":
                damage = (int)(damage * 1.15);
                bonusText = " *(Precision Shot)*";
                break;
            case "Monk" when _rng.Next(100) < 30:
                damage = (int)(damage * 1.6);
                bonusText = " *(Flurry of Blows!)*";
                break;
            case "Druid":
                damage = (int)(damage * 1.1);
                bonusText = " *(Nature's Wrath)*";
                break;
            case "Bard":
                // Bard buffs are more supportive — still decent damage
                damage = (int)(damage * 1.05);
                bonusText = " *(Inspiring Strike)*";
                break;
            case "Cleric" when boss.Name.Contains("Lich") || boss.Name.Contains("Demon"):
                damage = (int)(damage * 1.6);
                bonusText = " *(Turn Undead!)*";
                break;
        }

        // Racial bonuses
        if (player.Race == "Orc" && _rng.Next(100) < 15)
        {
            damage = (int)(damage * 1.5);
            bonusText += " *(Savage Crit!)*";
        }
        if (player.Race == "Dragonborn" && _rng.Next(100) < 15)
        {
            var breathDmg = 10 + player.Level * 3;
            damage += breathDmg;
            bonusText += $" *(Breath Weapon +{breathDmg}!)*";
        }

        damage = Math.Max(1, damage);

        // Boss counterattack
        var bossAtk = (int)(boss.Attack * phase.AtkMult);
        var bossDmg = Math.Max(1, bossAtk - def / 2 + _rng.Next(-5, 10));
        var playerHpAfter = Math.Max(0, maxHp - bossDmg);

        // Apply damage to boss
        boss.CurrentHp = Math.Max(0, boss.CurrentHp - damage);
        var newHpPercent = boss.CurrentHp <= 0 ? 0.0 : (double)boss.CurrentHp / boss.MaxHp * 100;
        var newPhaseIdx = newHpPercent switch
        {
            > 75 => 0,
            > 50 => 1,
            > 25 => 2,
            _    => 3,
        };

        // Update boss in DB
        await ctx.GetTable<RaidBoss>()
            .Where(x => x.Id == boss.Id)
            .UpdateAsync(_ => new RaidBoss
            {
                CurrentHp = boss.CurrentHp,
                CurrentPhase = newPhaseIdx + 1,
                IsActive = boss.CurrentHp > 0,
                DefeatedAt = boss.CurrentHp <= 0 ? DateTime.UtcNow : null,
            });

        // Track participation
        var participant = await ctx.GetTable<RaidBossParticipant>()
            .FirstOrDefaultAsyncLinqToDB(x => x.RaidBossId == boss.Id && x.UserId == userId);

        if (participant is null)
        {
            ctx.Add(new RaidBossParticipant
            {
                RaidBossId = boss.Id,
                UserId = userId,
                GuildId = guildId,
                DamageDealt = damage,
                HitCount = 1,
            });
            await ctx.SaveChangesAsync();
        }
        else
        {
            await ctx.GetTable<RaidBossParticipant>()
                .Where(x => x.Id == participant.Id)
                .UpdateAsync(_ => new RaidBossParticipant
                {
                    DamageDealt = participant.DamageDealt + damage,
                    HitCount = participant.HitCount + 1,
                    LastAttackAt = DateTime.UtcNow,
                });
        }

        // Set cooldown
        _attackCooldowns[cooldownKey] = DateTime.UtcNow;

        var sb = new StringBuilder();
        sb.AppendLine($"{classEmoji} **{username}** attacks **{boss.Emoji} {boss.Name}** for **{damage}** damage!{bonusText}");

        // Boss hits back
        sb.AppendLine($"{boss.Emoji} The boss retaliates for **{bossDmg}** damage! (You: {playerHpAfter}/{maxHp} HP)");

        // Phase transition
        if (newPhaseIdx > phaseIdx)
        {
            var newPhase = Phases[newPhaseIdx];
            sb.AppendLine($"\n**PHASE {newPhaseIdx + 1}: {newPhase.Name.ToUpper()}!**");
            sb.AppendLine($"The boss grows more powerful! ATK x{newPhase.AtkMult} | DEF x{newPhase.DefMult}");
        }

        // HP bar
        var barLen = 20;
        var filled = boss.MaxHp > 0 ? (int)(boss.CurrentHp * barLen / boss.MaxHp) : 0;
        filled = Math.Clamp(filled, 0, barLen);
        var hpBar = new string('█', filled) + new string('░', barLen - filled);
        sb.AppendLine($"\n[{hpBar}] {boss.CurrentHp:N0}/{boss.MaxHp:N0} HP");

        // Boss defeated!
        if (boss.CurrentHp <= 0)
        {
            sb.AppendLine($"\n# {boss.Emoji} RAID BOSS DEFEATED!");
            sb.AppendLine($"**{boss.Name}** has been slain!\n");

            // Distribute rewards
            var participants = await ctx.GetTable<RaidBossParticipant>()
                .Where(x => x.RaidBossId == boss.Id)
                .OrderByDescending(x => x.DamageDealt)
                .ToListAsyncLinqToDB();

            var totalDmg = participants.Sum(p => p.DamageDealt);
            sb.AppendLine($"**{participants.Count}** warriors participated! Total damage: **{totalDmg:N0}**\n");
            sb.AppendLine("**Top Damage:**");

            var rank = 1;
            foreach (var p in participants.Take(10))
            {
                var pct = totalDmg > 0 ? (double)p.DamageDealt / totalDmg * 100 : 0;
                var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" };

                // Scale rewards by damage contribution
                var xpShare = (long)(boss.XpReward * (p.DamageDealt / (double)totalDmg));
                var currShare = (long)(boss.CurrencyReward * (p.DamageDealt / (double)totalDmg));

                // Top 3 get bonus
                if (rank <= 3)
                {
                    var bonusMult = rank switch { 1 => 2.0, 2 => 1.5, 3 => 1.25, _ => 1.0 };
                    xpShare = (long)(xpShare * bonusMult);
                    currShare = (long)(currShare * bonusMult);
                }

                // Award currency
                await _cs.AddAsync(p.UserId, currShare, new TxData("raidboss", boss.Name));

                // Award XP
                var dPlayer = await ctx.GetTable<DungeonPlayer>()
                    .FirstOrDefaultAsyncLinqToDB(x => x.UserId == p.UserId && x.GuildId == guildId);
                if (dPlayer is not null)
                {
                    dPlayer.Xp += xpShare;
                    var newLevel = DungeonService.CalculateLevel(dPlayer.Xp);
                    if (newLevel > dPlayer.Level)
                    {
                        var gained = newLevel - dPlayer.Level;
                        dPlayer.Level = newLevel;
                        dPlayer.BaseHp += gained * 5;
                        dPlayer.BaseAttack += gained * 2;
                        dPlayer.BaseDefense += gained;
                    }
                    await ctx.GetTable<DungeonPlayer>()
                        .Where(x => x.Id == dPlayer.Id)
                        .UpdateAsync(_ => new DungeonPlayer
                        {
                            Xp = dPlayer.Xp,
                            Level = dPlayer.Level,
                            BaseHp = dPlayer.BaseHp,
                            BaseAttack = dPlayer.BaseAttack,
                            BaseDefense = dPlayer.BaseDefense,
                        });
                }

                sb.AppendLine($"  {medal} <@{p.UserId}> — **{p.DamageDealt:N0}** dmg ({pct:F1}%) | +{xpShare} XP | +{currShare} currency");
                rank++;
            }

            if (participants.Count > 10)
                sb.AppendLine($"  ...and **{participants.Count - 10}** more warriors!");

            // Loot drops for top 5
            sb.AppendLine("\n**Boss Loot Drops:**");
            foreach (var p in participants.Take(5))
            {
                if (_rng.Next(100) < 40 + (rank <= 3 ? 20 : 0))
                {
                    var item = GenerateRaidLoot(p.UserId, guildId, boss.Name);
                    ctx.Add(item);
                    await ctx.SaveChangesAsync();
                    sb.AppendLine($"  {DungeonService.RarityEmoji(item.Rarity)} <@{p.UserId}> received: **{item.Name}** ({item.Rarity} {item.Slot})");
                }
            }
        }

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  STATUS
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message, RaidBoss Boss)> GetStatusAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var boss = await ctx.GetTable<RaidBoss>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);

        if (boss is null)
            return (false, "No active raid boss right now.", null);

        var participants = await ctx.GetTable<RaidBossParticipant>()
            .Where(x => x.RaidBossId == boss.Id)
            .OrderByDescending(x => x.DamageDealt)
            .ToListAsyncLinqToDB();

        var totalDmg = participants.Sum(p => p.DamageDealt);
        var hpPercent = (double)boss.CurrentHp / boss.MaxHp * 100;
        var phaseIdx = hpPercent switch { > 75 => 0, > 50 => 1, > 25 => 2, _ => 3 };
        var phase = Phases[phaseIdx];

        var barLen = 20;
        var filled = boss.MaxHp > 0 ? (int)(boss.CurrentHp * barLen / boss.MaxHp) : 0;
        filled = Math.Clamp(filled, 0, barLen);
        var hpBar = new string('█', filled) + new string('░', barLen - filled);

        var sb = new StringBuilder();
        sb.AppendLine($"# {boss.Emoji} {boss.Name}");
        sb.AppendLine($"**Phase {phaseIdx + 1}: {phase.Name}** | ATK: {(int)(boss.Attack * phase.AtkMult)} | DEF: {(int)(boss.Defense * phase.DefMult)}");
        sb.AppendLine($"\n[{hpBar}] {boss.CurrentHp:N0}/{boss.MaxHp:N0} HP ({hpPercent:F1}%)");
        sb.AppendLine($"\n**{participants.Count}** warriors fighting | Total damage: **{totalDmg:N0}**");

        if (participants.Count > 0)
        {
            sb.AppendLine("\n**Top Damage:**");
            var rank = 1;
            foreach (var p in participants.Take(5))
            {
                var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" };
                sb.AppendLine($"  {medal} <@{p.UserId}> — **{p.DamageDealt:N0}** ({p.HitCount} hits)");
                rank++;
            }
        }

        sb.AppendLine($"\nSpawned: <t:{new DateTimeOffset(boss.SpawnedAt).ToUnixTimeSeconds()}:R>");
        sb.AppendLine("Use `.raidboss attack` to fight!");

        return (true, sb.ToString(), boss);
    }

    // ═══════════════════════════════════════════════════════════
    //  LEADERBOARD
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> GetLeaderboardAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Aggregate lifetime raid damage per user
        var topPlayers = await ctx.GetTable<RaidBossParticipant>()
            .Where(x => x.GuildId == guildId)
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalDamage = g.Sum(x => x.DamageDealt),
                TotalHits = g.Sum(x => x.HitCount),
                RaidsJoined = g.Count(),
            })
            .OrderByDescending(x => x.TotalDamage)
            .Take(15)
            .ToListAsyncLinqToDB();

        if (topPlayers.Count == 0)
            return (false, "No raid boss history yet! Spawn one with `.raidboss spawn`.");

        var totalBosses = await ctx.GetTable<RaidBoss>()
            .Where(x => x.GuildId == guildId && !x.IsActive)
            .CountAsyncLinqToDB();

        var sb = new StringBuilder();
        sb.AppendLine("# Raid Boss Leaderboard");
        sb.AppendLine($"**{totalBosses}** bosses defeated\n");

        var rank = 1;
        foreach (var p in topPlayers)
        {
            var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" };
            sb.AppendLine($"{medal} <@{p.UserId}> — **{p.TotalDamage:N0}** total dmg | {p.RaidsJoined} raids | {p.TotalHits} hits");
            rank++;
        }

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  RANDOM SPAWN LOGIC (called from DungeonService)
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Spawned, string Message, ulong ChannelId)> TryRandomSpawnAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<RaidBossConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
        {
            // Create default config
            config = new RaidBossConfig
            {
                GuildId = guildId,
                NextSpawnThreshold = _rng.Next(5, 26),
            };
            ctx.Add(config);
            await ctx.SaveChangesAsync();
            return (false, null, 0);
        }

        if (!config.RandomSpawnsEnabled || config.SpawnChannelId == 0)
            return (false, null, 0);

        // Increment counter
        config.DungeonClearsSinceLastRaid++;

        if (config.DungeonClearsSinceLastRaid >= config.NextSpawnThreshold)
        {
            // Check no active boss
            var activeBoss = await ctx.GetTable<RaidBoss>()
                .AnyAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);

            if (activeBoss)
            {
                // Reset counter but don't spawn
                await ctx.GetTable<RaidBossConfig>()
                    .Where(x => x.Id == config.Id)
                    .UpdateAsync(_ => new RaidBossConfig { DungeonClearsSinceLastRaid = config.DungeonClearsSinceLastRaid });
                return (false, null, 0);
            }

            // Reset counter and roll new threshold
            var newThreshold = _rng.Next(config.MinDungeonClears, config.MaxDungeonClears + 1);
            await ctx.GetTable<RaidBossConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(_ => new RaidBossConfig
                {
                    DungeonClearsSinceLastRaid = 0,
                    NextSpawnThreshold = newThreshold,
                });

            // Spawn the boss!
            var (success, message) = await SpawnBossAsync(guildId, config.SpawnChannelId, null, isRandom: true);
            return (success, message, config.SpawnChannelId);
        }

        // Just save the incremented counter
        await ctx.GetTable<RaidBossConfig>()
            .Where(x => x.Id == config.Id)
            .UpdateAsync(_ => new RaidBossConfig { DungeonClearsSinceLastRaid = config.DungeonClearsSinceLastRaid });

        return (false, null, 0);
    }

    // ═══════════════════════════════════════════════════════════
    //  CONFIGURE RANDOM SPAWNS
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> ConfigureAsync(ulong guildId, ulong channelId, int minClears, int maxClears, bool enabled)
    {
        minClears = Math.Clamp(minClears, 2, 1000);
        maxClears = Math.Clamp(maxClears, minClears, 1000);

        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<RaidBossConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
        {
            config = new RaidBossConfig
            {
                GuildId = guildId,
                SpawnChannelId = channelId,
                MinDungeonClears = minClears,
                MaxDungeonClears = maxClears,
                RandomSpawnsEnabled = enabled,
                NextSpawnThreshold = _rng.Next(minClears, maxClears + 1),
            };
            ctx.Add(config);
            await ctx.SaveChangesAsync();
        }
        else
        {
            await ctx.GetTable<RaidBossConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(_ => new RaidBossConfig
                {
                    SpawnChannelId = channelId,
                    MinDungeonClears = minClears,
                    MaxDungeonClears = maxClears,
                    RandomSpawnsEnabled = enabled,
                    NextSpawnThreshold = _rng.Next(minClears, maxClears + 1),
                });
        }

        return (true, $"Raid boss random spawns **{(enabled ? "enabled" : "disabled")}**!\n" +
            $"Channel: <#{channelId}>\n" +
            $"Spawns after **{minClears}-{maxClears}** dungeon completions (randomized)");
    }

    // ═══════════════════════════════════════════════════════════
    //  RAID-EXCLUSIVE LOOT
    // ═══════════════════════════════════════════════════════════

    private static readonly (string Name, string Slot, int Atk, int Def, int Hp, string Special)[] RaidLootTable =
    [
        ("Dragon Slayer",         "Weapon",    60, 10, 30,  "Raid Boss DMG +20%"),
        ("Lich King's Crown",     "Accessory", 20, 20, 80,  "XP +30%"),
        ("Kraken Tentacle Whip",  "Weapon",    55, 0,  0,   "Splash DMG 15%"),
        ("Void Walker Armor",     "Armor",     15, 50, 100, "Phase Resist"),
        ("Titan's Gauntlet",      "Accessory", 40, 25, 50,  "Stun 10%"),
        ("Phoenix Flame Robes",   "Armor",     20, 35, 60,  "Auto-Revive"),
        ("Hydra Fang Dagger",     "Weapon",    45, 0,  0,   "Multi-Strike 20%"),
        ("Demon Lord's Signet",   "Accessory", 30, 30, 70,  "Lifesteal 15%"),
        ("Frost Wyrm Scale Mail", "Armor",     10, 55, 90,  "Freeze Counter 10%"),
        ("Abyssal Core",          "Accessory", 50, 15, 40,  "All Stats +10%"),
    ];

    private DungeonItem GenerateRaidLoot(ulong userId, ulong guildId, string bossName)
    {
        var template = RaidLootTable[_rng.Next(RaidLootTable.Length)];

        // Raid loot is always Epic or Legendary
        var rarity = _rng.Next(100) < 30 ? "Legendary" : "Epic";
        var mult = rarity == "Legendary" ? 3.0 : 2.0;

        return new DungeonItem
        {
            UserId = userId,
            GuildId = guildId,
            Name = rarity == "Legendary" ? $"Legendary {template.Name}" : template.Name,
            Slot = template.Slot,
            Rarity = rarity,
            BonusAttack = (int)(template.Atk * mult),
            BonusDefense = (int)(template.Def * mult),
            BonusHp = (int)(template.Hp * mult),
            SpecialEffect = template.Special,
        };
    }
}
