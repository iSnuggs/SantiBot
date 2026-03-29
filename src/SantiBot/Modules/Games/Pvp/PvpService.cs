#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Modules.Games.Dungeon;
using SantiBot.Services.Currency;
using System.Text;

namespace SantiBot.Modules.Games.Pvp;

public sealed class PvpService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private readonly DungeonService _dungeon;
    private static readonly SantiRandom _rng = new();

    // Active duel challenges: challengerId -> (targetId, guildId, timestamp)
    private readonly ConcurrentDictionary<ulong, (ulong TargetId, ulong GuildId, DateTime IssuedAt)> _pendingChallenges = new();

    public PvpService(DbService db, ICurrencyService cs, DungeonService dungeon)
    {
        _db = db;
        _cs = cs;
        _dungeon = dungeon;
    }

    // ═══════════════════════════════════════════════════════════
    //  ELO CALCULATION
    // ═══════════════════════════════════════════════════════════

    private const int KFactor = 32;

    private static (int WinnerNew, int LoserNew) CalculateElo(int winnerElo, int loserElo)
    {
        var expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserElo - winnerElo) / 400.0));
        var expectedLoser = 1.0 - expectedWinner;

        var newWinner = winnerElo + (int)Math.Round(KFactor * (1.0 - expectedWinner));
        var newLoser = loserElo + (int)Math.Round(KFactor * (0.0 - expectedLoser));
        newLoser = Math.Max(100, newLoser); // floor at 100

        return (newWinner, newLoser);
    }

    // ═══════════════════════════════════════════════════════════
    //  PVP STATS DB
    // ═══════════════════════════════════════════════════════════

    private async Task<PvpStats> GetOrCreateStatsAsync(ulong userId, ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var stats = await ctx.GetTable<PvpStats>()
            .FirstOrDefaultAsyncLinqToDB(x => x.UserId == userId && x.GuildId == guildId);

        if (stats is not null)
            return stats;

        stats = new PvpStats { UserId = userId, GuildId = guildId };
        ctx.Add(stats);
        await ctx.SaveChangesAsync();
        return stats;
    }

    private async Task SaveStatsAsync(PvpStats stats)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<PvpStats>()
            .Where(x => x.Id == stats.Id)
            .UpdateAsync(_ => new PvpStats
            {
                Wins = stats.Wins,
                Losses = stats.Losses,
                Draws = stats.Draws,
                Elo = stats.Elo,
                WinStreak = stats.WinStreak,
                BestWinStreak = stats.BestWinStreak,
                TotalDamageDealt = stats.TotalDamageDealt,
                TotalDamageReceived = stats.TotalDamageReceived,
                LastDuelAt = stats.LastDuelAt,
            });
    }

    // ═══════════════════════════════════════════════════════════
    //  DUEL SYSTEM
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message, EmbedBuilder Embed)> Duel(
        ulong challengerId, string challengerName,
        ulong targetId, string targetName,
        ulong guildId)
    {
        if (challengerId == targetId)
            return (false, "You can't duel yourself!", null);

        // Cooldown check
        var challengerStats = await GetOrCreateStatsAsync(challengerId, guildId);
        if (challengerStats.LastDuelAt.HasValue
            && (DateTime.UtcNow - challengerStats.LastDuelAt.Value).TotalMinutes < 5)
        {
            var remaining = 5 - (DateTime.UtcNow - challengerStats.LastDuelAt.Value).TotalMinutes;
            return (false, $"You must wait **{remaining:F1} minutes** before dueling again.", null);
        }

        var targetStats = await GetOrCreateStatsAsync(targetId, guildId);
        if (targetStats.LastDuelAt.HasValue
            && (DateTime.UtcNow - targetStats.LastDuelAt.Value).TotalMinutes < 5)
        {
            return (false, $"**{targetName}** is still on cooldown from their last duel.", null);
        }

        // Get dungeon characters
        var p1 = await _dungeon.GetOrCreatePlayerAsync(challengerId, guildId);
        var p1Equip = await _dungeon.GetEquippedItemsAsync(challengerId, guildId);
        var (p1MaxHp, p1Atk, p1Def) = DungeonService.GetEffectiveStats(p1, p1Equip);

        var p2 = await _dungeon.GetOrCreatePlayerAsync(targetId, guildId);
        var p2Equip = await _dungeon.GetEquippedItemsAsync(targetId, guildId);
        var (p2MaxHp, p2Atk, p2Def) = DungeonService.GetEffectiveStats(p2, p2Equip);

        // Run the fight
        var (winnerId, log, p1DmgDealt, p2DmgDealt) = SimulateDuel(
            challengerId, challengerName, p1.Class, p1.Race, p1MaxHp, p1Atk, p1Def,
            targetId, targetName, p2.Class, p2.Race, p2MaxHp, p2Atk, p2Def);

        var isDraw = winnerId == 0;
        var winnerName = winnerId == challengerId ? challengerName : targetName;

        // Update stats
        long currencyReward = 0;
        if (isDraw)
        {
            challengerStats.Draws++;
            targetStats.Draws++;
            challengerStats.WinStreak = 0;
            targetStats.WinStreak = 0;
        }
        else
        {
            var winStats = winnerId == challengerId ? challengerStats : targetStats;
            var loseStats = winnerId == challengerId ? targetStats : challengerStats;

            winStats.Wins++;
            loseStats.Losses++;
            winStats.WinStreak++;
            loseStats.WinStreak = 0;

            if (winStats.WinStreak > winStats.BestWinStreak)
                winStats.BestWinStreak = winStats.WinStreak;

            var (newWinElo, newLoseElo) = CalculateElo(winStats.Elo, loseStats.Elo);
            var eloGain = newWinElo - winStats.Elo;
            winStats.Elo = newWinElo;
            loseStats.Elo = newLoseElo;

            // Currency: base 50 + 10 per Elo point gained
            currencyReward = 50 + eloGain * 10;
            await _cs.AddAsync(winnerId, currencyReward, new TxData("pvp", "duel-win"));
        }

        challengerStats.TotalDamageDealt += p1DmgDealt;
        challengerStats.TotalDamageReceived += p2DmgDealt;
        targetStats.TotalDamageDealt += p2DmgDealt;
        targetStats.TotalDamageReceived += p1DmgDealt;
        challengerStats.LastDuelAt = DateTime.UtcNow;
        targetStats.LastDuelAt = DateTime.UtcNow;

        await SaveStatsAsync(challengerStats);
        await SaveStatsAsync(targetStats);

        // Build embed
        var eb = new EmbedBuilder()
            .WithTitle($"PvP Duel: {challengerName} vs {targetName}")
            .WithDescription(log)
            .WithColor(isDraw ? new Color(255, 255, 0) : new Color(0, 200, 0));

        var resultText = isDraw
            ? "It's a **DRAW**!"
            : $"**{winnerName}** wins!";

        if (currencyReward > 0)
            resultText += $"\n+{currencyReward} currency earned!";

        eb.AddField("Result", resultText, false);
        eb.AddField($"{challengerName}", $"Elo: **{challengerStats.Elo}** | W/L: {challengerStats.Wins}/{challengerStats.Losses}", true);
        eb.AddField($"{targetName}", $"Elo: **{targetStats.Elo}** | W/L: {targetStats.Wins}/{targetStats.Losses}", true);

        return (true, null, eb);
    }

    private (ulong WinnerId, string Log, long P1DmgDealt, long P2DmgDealt) SimulateDuel(
        ulong p1Id, string p1Name, string p1Class, string p1Race, int p1Hp, int p1Atk, int p1Def,
        ulong p2Id, string p2Name, string p2Class, string p2Race, int p2Hp, int p2Atk, int p2Def)
    {
        var sb = new StringBuilder();
        var p1CurrHp = p1Hp;
        var p2CurrHp = p2Hp;
        long p1TotalDmg = 0;
        long p2TotalDmg = 0;
        var maxTurns = 30;
        var turn = 0;

        var p1ClassEmoji = DungeonService.Classes.TryGetValue(p1Class, out var c1) ? c1.Emoji : "⚔️";
        var p2ClassEmoji = DungeonService.Classes.TryGetValue(p2Class, out var c2) ? c2.Emoji : "⚔️";

        sb.AppendLine($"{p1ClassEmoji} **{p1Name}** ({p1Class} {p1Race}) — HP: {p1Hp} | ATK: {p1Atk} | DEF: {p1Def}");
        sb.AppendLine($"{p2ClassEmoji} **{p2Name}** ({p2Class} {p2Race}) — HP: {p2Hp} | ATK: {p2Atk} | DEF: {p2Def}");
        sb.AppendLine("───────────────────");

        while (p1CurrHp > 0 && p2CurrHp > 0 && turn < maxTurns)
        {
            turn++;
            sb.AppendLine($"**Turn {turn}:**");

            // Player 1 attacks Player 2
            var (p1Hit, p1Ability) = CalculateAttack(p1Atk, p2Def, p1Class, p1Race, turn);
            p2CurrHp -= p1Hit;
            p1TotalDmg += p1Hit;
            var p1Desc = p1Ability is not null ? $"uses **{p1Ability}** for" : "attacks for";
            sb.AppendLine($"  {p1ClassEmoji} {p1Name} {p1Desc} **{p1Hit}** dmg! ({Math.Max(0, p2CurrHp)}/{p2Hp} HP)");

            // Class passive: Warrior Second Wind
            if (p1Class == "Warrior" && p1CurrHp <= p1Hp * 0.3 && _rng.Next(100) < 25)
            {
                var heal = p1Hp / 5;
                p1CurrHp = Math.Min(p1Hp, p1CurrHp + heal);
                sb.AppendLine($"  {p1ClassEmoji} {p1Name} triggers **Second Wind**, healing {heal} HP!");
            }

            if (p2CurrHp <= 0) break;

            // Player 2 attacks Player 1
            var (p2Hit, p2Ability) = CalculateAttack(p2Atk, p1Def, p2Class, p2Race, turn);
            p1CurrHp -= p2Hit;
            p2TotalDmg += p2Hit;
            var p2Desc = p2Ability is not null ? $"uses **{p2Ability}** for" : "attacks for";
            sb.AppendLine($"  {p2ClassEmoji} {p2Name} {p2Desc} **{p2Hit}** dmg! ({Math.Max(0, p1CurrHp)}/{p1Hp} HP)");

            // Class passive: Warrior Second Wind
            if (p2Class == "Warrior" && p2CurrHp <= p2Hp * 0.3 && _rng.Next(100) < 25)
            {
                var heal = p2Hp / 5;
                p2CurrHp = Math.Min(p2Hp, p2CurrHp + heal);
                sb.AppendLine($"  {p2ClassEmoji} {p2Name} triggers **Second Wind**, healing {heal} HP!");
            }

            // Class passive: Cleric heal
            if (p1Class == "Cleric" && _rng.Next(100) < 20)
            {
                var heal = p1Atk / 2;
                p1CurrHp = Math.Min(p1Hp, p1CurrHp + heal);
                sb.AppendLine($"  ✝️ {p1Name} casts **Divine Heal** for {heal} HP!");
            }
            if (p2Class == "Cleric" && _rng.Next(100) < 20)
            {
                var heal = p2Atk / 2;
                p2CurrHp = Math.Min(p2Hp, p2CurrHp + heal);
                sb.AppendLine($"  ✝️ {p2Name} casts **Divine Heal** for {heal} HP!");
            }

            // Class passive: Necromancer Life Drain
            if (p1Class == "Necromancer" && _rng.Next(100) < 15)
            {
                var drain = p1Atk / 3;
                p2CurrHp -= drain;
                p1CurrHp = Math.Min(p1Hp, p1CurrHp + drain);
                p1TotalDmg += drain;
                sb.AppendLine($"  💀 {p1Name} drains **{drain}** life from {p2Name}!");
            }
            if (p2Class == "Necromancer" && _rng.Next(100) < 15)
            {
                var drain = p2Atk / 3;
                p1CurrHp -= drain;
                p2CurrHp = Math.Min(p2Hp, p2CurrHp + drain);
                p2TotalDmg += drain;
                sb.AppendLine($"  💀 {p2Name} drains **{drain}** life from {p1Name}!");
            }

            sb.AppendLine();
        }

        ulong winnerId;
        if (p1CurrHp <= 0 && p2CurrHp <= 0)
            winnerId = 0; // draw
        else if (p1CurrHp <= 0)
            winnerId = p2Id;
        else if (p2CurrHp <= 0)
            winnerId = p1Id;
        else
            winnerId = p1CurrHp >= p2CurrHp ? p1Id : p2Id; // timeout: higher HP wins

        return (winnerId, sb.ToString(), p1TotalDmg, p2TotalDmg);
    }

    private (int Damage, string AbilityName) CalculateAttack(int atk, int def, string attackerClass, string attackerRace, int turn)
    {
        // Base damage with variance
        var baseDmg = Math.Max(1, atk - def / 2);
        var variance = _rng.Next(-baseDmg / 5, baseDmg / 5 + 1);
        var damage = Math.Max(1, baseDmg + variance);
        string ability = null;

        // Class abilities
        switch (attackerClass)
        {
            case "Rogue" when _rng.Next(100) < 25:
                damage = (int)(damage * 1.8);
                ability = "Sneak Attack";
                break;
            case "Mage" when _rng.Next(100) < 20:
                damage = (int)(damage * 2.0);
                ability = "Fireball";
                break;
            case "Barbarian" when _rng.Next(100) < 30:
                damage = (int)(damage * 1.6);
                ability = "Reckless Attack";
                break;
            case "Monk" when _rng.Next(100) < 22:
                damage = (int)(damage * 1.5);
                ability = "Flurry of Blows";
                break;
            case "Ranger" when turn == 1:
                damage = (int)(damage * 1.7);
                ability = "First Strike";
                break;
            case "Paladin" when _rng.Next(100) < 18:
                damage = (int)(damage * 1.9);
                ability = "Divine Smite";
                break;
            case "Bard" when _rng.Next(100) < 20:
                damage = (int)(damage * 0.3);
                ability = "Vicious Mockery";
                break;
            case "Druid" when _rng.Next(100) < 18:
                damage = (int)(damage * 1.6);
                ability = "Wild Shape";
                break;
        }

        // Race abilities
        switch (attackerRace)
        {
            case "Orc" when _rng.Next(100) < 15:
                damage = (int)(damage * 2.5);
                ability = (ability ?? "") + " + Savage Crit";
                break;
            case "Dragonborn" when _rng.Next(100) < 15:
                damage += atk / 2;
                ability = (ability ?? "") + " + Breath Weapon";
                break;
            case "Tiefling" when _rng.Next(100) < 20:
                damage = (int)(damage * 1.4);
                ability = (ability ?? "") + " + Infernal Wrath";
                break;
        }

        return (Math.Max(1, damage), ability);
    }

    // ═══════════════════════════════════════════════════════════
    //  STATS & LEADERBOARD
    // ═══════════════════════════════════════════════════════════

    public async Task<PvpStats> GetPvpStats(ulong userId, ulong guildId)
        => await GetOrCreateStatsAsync(userId, guildId);

    public async Task<List<PvpStats>> GetLeaderboard(ulong guildId, int count = 10)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<PvpStats>()
            .Where(x => x.GuildId == guildId && (x.Wins + x.Losses) > 0)
            .OrderByDescending(x => x.Elo)
            .Take(count)
            .ToListAsyncLinqToDB();
    }

    // ═══════════════════════════════════════════════════════════
    //  TOURNAMENT SYSTEM
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> CreateTournament(
        ulong guildId, ulong channelId, ulong createdBy,
        string name, string format, int maxParticipants, long entryFee)
    {
        if (maxParticipants < 4 || maxParticipants > 64)
            return (false, "Tournament must have between 4 and 64 max participants.");

        var validFormats = new[] { "SingleElimination", "DoubleElimination", "RoundRobin" };
        if (!validFormats.Contains(format))
            return (false, $"Invalid format. Choose: {string.Join(", ", validFormats)}");

        // Check for existing active tournament in guild
        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<TournamentModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status != "Completed");

        if (existing is not null)
            return (false, $"There's already an active tournament: **{existing.Name}** ({existing.Status})");

        var tournament = new TournamentModel
        {
            GuildId = guildId,
            ChannelId = channelId,
            Name = name,
            Format = format,
            MaxParticipants = maxParticipants,
            EntryFee = entryFee,
            PrizePool = 0,
            CreatedBy = createdBy,
        };

        ctx.Add(tournament);
        await ctx.SaveChangesAsync();

        return (true, $"Tournament **{name}** created!\n" +
            $"Format: **{format}** | Max: **{maxParticipants}** players | Entry Fee: **{entryFee}** currency\n" +
            $"Players can join with `.pvp tournamentjoin`");
    }

    public async Task<(bool Success, string Message)> RegisterForTournament(
        ulong userId, ulong guildId, string username)
    {
        await using var ctx = _db.GetDbContext();
        var tournament = await ctx.GetTable<TournamentModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status == "Registration");

        if (tournament is null)
            return (false, "No tournament is currently open for registration.");

        // Check if already registered
        var existing = await ctx.GetTable<TournamentParticipant>()
            .FirstOrDefaultAsyncLinqToDB(x => x.TournamentId == tournament.Id && x.UserId == userId);

        if (existing is not null)
            return (false, "You're already registered for this tournament.");

        // Check capacity
        var currentCount = await ctx.GetTable<TournamentParticipant>()
            .CountAsyncLinqToDB(x => x.TournamentId == tournament.Id);

        if (currentCount >= tournament.MaxParticipants)
            return (false, "Tournament is full!");

        // Charge entry fee
        if (tournament.EntryFee > 0)
        {
            var paid = await _cs.RemoveAsync(userId, tournament.EntryFee, new TxData("pvp", "tournament-entry"));
            if (!paid)
                return (false, $"You don't have enough currency! Entry fee: **{tournament.EntryFee}**");
        }

        // Add participant
        var participant = new TournamentParticipant
        {
            TournamentId = tournament.Id,
            UserId = userId,
            GuildId = guildId,
            Seed = currentCount + 1,
        };

        ctx.Add(participant);

        // Update prize pool
        await ctx.GetTable<TournamentModel>()
            .Where(x => x.Id == tournament.Id)
            .UpdateAsync(_ => new TournamentModel
            {
                PrizePool = tournament.PrizePool + tournament.EntryFee,
            });

        await ctx.SaveChangesAsync();

        return (true, $"**{username}** has joined **{tournament.Name}**! " +
            $"({currentCount + 1}/{tournament.MaxParticipants} players)\n" +
            $"Prize Pool: **{tournament.PrizePool + tournament.EntryFee}** currency");
    }

    public async Task<(bool Success, string Message, List<(string WinnerName, string LoserName, string Log)> MatchResults)> StartTournament(
        ulong guildId, ulong startedBy)
    {
        await using var ctx = _db.GetDbContext();
        var tournament = await ctx.GetTable<TournamentModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status == "Registration");

        if (tournament is null)
            return (false, "No tournament in registration phase.", null);

        if (tournament.CreatedBy != startedBy)
            return (false, "Only the tournament creator can start it.", null);

        var participants = await ctx.GetTable<TournamentParticipant>()
            .Where(x => x.TournamentId == tournament.Id)
            .OrderBy(x => x.Seed)
            .ToListAsyncLinqToDB();

        if (participants.Count < 4)
            return (false, $"Need at least 4 participants to start (currently {participants.Count}).", null);

        // Shuffle seeds randomly
        var shuffled = participants.OrderBy(_ => _rng.Next()).ToList();
        for (var i = 0; i < shuffled.Count; i++)
        {
            shuffled[i].Seed = i + 1;
            await ctx.GetTable<TournamentParticipant>()
                .Where(x => x.Id == shuffled[i].Id)
                .UpdateAsync(_ => new TournamentParticipant { Seed = i + 1 });
        }

        // Update tournament status
        await ctx.GetTable<TournamentModel>()
            .Where(x => x.Id == tournament.Id)
            .UpdateAsync(_ => new TournamentModel
            {
                Status = "InProgress",
                StartedAt = DateTime.UtcNow,
            });

        // Run single elimination bracket
        var matchResults = new List<(string WinnerName, string LoserName, string Log)>();
        var alive = shuffled.ToList();

        var round = 1;
        while (alive.Count > 1)
        {
            var nextRound = new List<TournamentParticipant>();

            for (var i = 0; i < alive.Count - 1; i += 2)
            {
                var a = alive[i];
                var b = alive[i + 1];

                var result = await RunMatch(a.UserId, b.UserId, guildId);
                matchResults.Add(result);

                if (result.WinnerName == "Player1")
                {
                    // A won
                    a.Wins++;
                    b.Losses++;
                    b.IsEliminated = true;
                    nextRound.Add(a);
                }
                else
                {
                    // B won
                    b.Wins++;
                    a.Losses++;
                    a.IsEliminated = true;
                    nextRound.Add(b);
                }

                // Save participant updates
                await ctx.GetTable<TournamentParticipant>()
                    .Where(x => x.Id == a.Id)
                    .UpdateAsync(_ => new TournamentParticipant
                    {
                        Wins = a.Wins, Losses = a.Losses, IsEliminated = a.IsEliminated,
                    });
                await ctx.GetTable<TournamentParticipant>()
                    .Where(x => x.Id == b.Id)
                    .UpdateAsync(_ => new TournamentParticipant
                    {
                        Wins = b.Wins, Losses = b.Losses, IsEliminated = b.IsEliminated,
                    });
            }

            // If odd number, last person gets a bye
            if (alive.Count % 2 == 1)
                nextRound.Add(alive[^1]);

            alive = nextRound;
            round++;
        }

        // Determine winners (1st, 2nd, 3rd)
        var sortedParticipants = participants
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Losses)
            .ToList();

        var first = sortedParticipants.ElementAtOrDefault(0);
        var second = sortedParticipants.ElementAtOrDefault(1);
        var third = sortedParticipants.ElementAtOrDefault(2);

        // Distribute prizes
        var pool = tournament.PrizePool;
        if (first is not null && pool > 0)
        {
            var firstPrize = (long)(pool * 0.50);
            var secondPrize = (long)(pool * 0.30);
            var thirdPrize = (long)(pool * 0.20);

            await _cs.AddAsync(first.UserId, firstPrize, new TxData("pvp", "tournament-1st"));
            if (second is not null)
                await _cs.AddAsync(second.UserId, secondPrize, new TxData("pvp", "tournament-2nd"));
            if (third is not null)
                await _cs.AddAsync(third.UserId, thirdPrize, new TxData("pvp", "tournament-3rd"));
        }

        // Mark completed
        await ctx.GetTable<TournamentModel>()
            .Where(x => x.Id == tournament.Id)
            .UpdateAsync(_ => new TournamentModel
            {
                Status = "Completed",
                CompletedAt = DateTime.UtcNow,
            });

        return (true,
            $"Tournament **{tournament.Name}** is complete!\n" +
            $"Prize Pool: **{pool}** currency\n\n" +
            $"🥇 1st: <@{first?.UserId}> — {(long)(pool * 0.50)} currency\n" +
            $"🥈 2nd: <@{second?.UserId}> — {(long)(pool * 0.30)} currency\n" +
            $"🥉 3rd: <@{third?.UserId}> — {(long)(pool * 0.20)} currency",
            matchResults);
    }

    public async Task<(string WinnerName, string LoserName, string Log)> RunMatch(
        ulong p1Id, ulong p2Id, ulong guildId)
    {
        var p1 = await _dungeon.GetOrCreatePlayerAsync(p1Id, guildId);
        var p1Equip = await _dungeon.GetEquippedItemsAsync(p1Id, guildId);
        var (p1MaxHp, p1Atk, p1Def) = DungeonService.GetEffectiveStats(p1, p1Equip);

        var p2 = await _dungeon.GetOrCreatePlayerAsync(p2Id, guildId);
        var p2Equip = await _dungeon.GetEquippedItemsAsync(p2Id, guildId);
        var (p2MaxHp, p2Atk, p2Def) = DungeonService.GetEffectiveStats(p2, p2Equip);

        var (winnerId, log, _, _) = SimulateDuel(
            p1Id, "Player1", p1.Class, p1.Race, p1MaxHp, p1Atk, p1Def,
            p2Id, "Player2", p2.Class, p2.Race, p2MaxHp, p2Atk, p2Def);

        if (winnerId == p1Id || winnerId == 0)
            return ("Player1", "Player2", log);
        else
            return ("Player2", "Player1", log);
    }

    public async Task<(bool Success, string Message)> GetTournamentBracket(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var tournament = await ctx.GetTable<TournamentModel>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Status != "Completed");

        if (tournament is null)
        {
            // Try to get the latest completed one
            tournament = await ctx.GetTable<TournamentModel>()
                .Where(x => x.GuildId == guildId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsyncLinqToDB();
        }

        if (tournament is null)
            return (false, "No tournaments found in this server.");

        var participants = await ctx.GetTable<TournamentParticipant>()
            .Where(x => x.TournamentId == tournament.Id)
            .OrderBy(x => x.Seed)
            .ToListAsyncLinqToDB();

        var sb = new StringBuilder();
        sb.AppendLine($"**{tournament.Name}** — {tournament.Format} ({tournament.Status})");
        sb.AppendLine($"Prize Pool: **{tournament.PrizePool}** currency");
        sb.AppendLine("───────────────────");

        foreach (var p in participants)
        {
            var status = p.IsEliminated ? "❌ ELIMINATED" : "✅ ALIVE";
            sb.AppendLine($"[Seed {p.Seed}] <@{p.UserId}> — W: {p.Wins} / L: {p.Losses} — {status}");
        }

        return (true, sb.ToString());
    }

    public async Task<(bool Success, string Message)> GetTournamentInfo(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var tournament = await ctx.GetTable<TournamentModel>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsyncLinqToDB();

        if (tournament is null)
            return (false, "No tournaments found in this server.");

        var participantCount = await ctx.GetTable<TournamentParticipant>()
            .CountAsyncLinqToDB(x => x.TournamentId == tournament.Id);

        var sb = new StringBuilder();
        sb.AppendLine($"**{tournament.Name}**");
        sb.AppendLine($"Status: **{tournament.Status}**");
        sb.AppendLine($"Format: **{tournament.Format}**");
        sb.AppendLine($"Players: **{participantCount}/{tournament.MaxParticipants}**");
        sb.AppendLine($"Entry Fee: **{tournament.EntryFee}** currency");
        sb.AppendLine($"Prize Pool: **{tournament.PrizePool}** currency");
        sb.AppendLine($"Created by: <@{tournament.CreatedBy}>");
        if (tournament.StartedAt.HasValue)
            sb.AppendLine($"Started: {tournament.StartedAt.Value:yyyy-MM-dd HH:mm} UTC");
        if (tournament.CompletedAt.HasValue)
            sb.AppendLine($"Completed: {tournament.CompletedAt.Value:yyyy-MM-dd HH:mm} UTC");

        return (true, sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    //  TEAM SYSTEM
    // ═══════════════════════════════════════════════════════════

    public (bool Success, string Message) RandomTeams(List<(ulong UserId, string Username)> users, int teamCount)
    {
        if (teamCount < 2 || teamCount > users.Count)
            return (false, $"Team count must be between 2 and {users.Count}.");

        var shuffled = users.OrderBy(_ => _rng.Next()).ToList();
        var teams = new List<List<(ulong UserId, string Username)>>();
        for (var i = 0; i < teamCount; i++)
            teams.Add(new());

        for (var i = 0; i < shuffled.Count; i++)
            teams[i % teamCount].Add(shuffled[i]);

        var sb = new StringBuilder();
        sb.AppendLine("**Random Teams:**");
        for (var i = 0; i < teams.Count; i++)
        {
            sb.AppendLine($"\n**Team {i + 1}:**");
            foreach (var (_, name) in teams[i])
                sb.AppendLine($"  - {name}");
        }

        return (true, sb.ToString());
    }

    public async Task<(bool Success, string Message)> BalancedTeams(
        List<(ulong UserId, string Username)> users, int teamCount, ulong guildId)
    {
        if (teamCount < 2 || teamCount > users.Count)
            return (false, $"Team count must be between 2 and {users.Count}.");

        // Get dungeon levels for all users
        var playerLevels = new List<(ulong UserId, string Username, int Level)>();
        foreach (var (userId, username) in users)
        {
            var player = await _dungeon.GetOrCreatePlayerAsync(userId, guildId);
            playerLevels.Add((userId, username, player.Level));
        }

        // Sort by level descending, then distribute snake-draft style for balance
        var sorted = playerLevels.OrderByDescending(x => x.Level).ToList();
        var teams = new List<List<(ulong UserId, string Username, int Level)>>();
        for (var i = 0; i < teamCount; i++)
            teams.Add(new());

        // Snake draft: 1,2,3,...,N,N,...,3,2,1,1,2,3,...
        var direction = 1;
        var teamIdx = 0;
        foreach (var p in sorted)
        {
            teams[teamIdx].Add(p);
            teamIdx += direction;
            if (teamIdx >= teamCount)
            {
                teamIdx = teamCount - 1;
                direction = -1;
            }
            else if (teamIdx < 0)
            {
                teamIdx = 0;
                direction = 1;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("**Balanced Teams (by Dungeon Level):**");
        for (var i = 0; i < teams.Count; i++)
        {
            var totalLevel = teams[i].Sum(x => x.Level);
            sb.AppendLine($"\n**Team {i + 1}** (Total Level: {totalLevel}):");
            foreach (var (_, name, level) in teams[i])
                sb.AppendLine($"  - {name} (Lv. {level})");
        }

        return (true, sb.ToString());
    }
}
