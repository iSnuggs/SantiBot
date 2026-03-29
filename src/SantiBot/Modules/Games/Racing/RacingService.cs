#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

using System.Text;

namespace SantiBot.Modules.Games.Racing;

public sealed class RacingService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private static readonly SantiRandom _rng = new();
    public readonly ConcurrentDictionary<ulong, RaceGame> ActiveRaces = new();

    public RacingService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public class RaceGame
    {
        public ulong ChannelId { get; set; }
        public List<(ulong UserId, string Username, long Bet)> Racers { get; set; } = new();
        public bool Started { get; set; }
    }

    private async Task<RaceCar> GetOrCreateCar(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var car = await ctx.GetTable<RaceCar>()
            .FirstOrDefaultAsyncLinqToDB(c => c.UserId == userId);

        if (car is null)
        {
            await ctx.GetTable<RaceCar>().InsertAsync(() => new RaceCar
            {
                UserId = userId,
                Speed = 10,
                Handling = 10,
                Nitro = 10,
                Wins = 0,
                Races = 0,
                DateAdded = DateTime.UtcNow
            });
            car = await ctx.GetTable<RaceCar>()
                .FirstOrDefaultAsyncLinqToDB(c => c.UserId == userId);
        }

        return car;
    }

    public async Task<(bool Success, string Message)> JoinRace(ulong channelId, ulong userId, string username, long bet)
    {
        if (bet < 0) return (false, "Bet can't be negative!");

        var game = ActiveRaces.GetOrAdd(channelId, _ => new RaceGame { ChannelId = channelId });
        if (game.Started) return (false, "Race already started!");
        if (game.Racers.Any(r => r.UserId == userId)) return (false, "Already in the race!");

        if (bet > 0)
        {
            var removed = await _cs.RemoveAsync(userId, bet, new TxData("race", "entry"));
            if (!removed) return (false, $"You need {bet} 🥠!");
        }

        game.Racers.Add((userId, username, bet));
        return (true, $"🏎️ {username} joins the race! (Bet: {bet} 🥠) — {game.Racers.Count} racers\nUse `.race start` when ready!");
    }

    public async Task<(bool Success, string Message)> StartRace(ulong channelId)
    {
        if (!ActiveRaces.TryGetValue(channelId, out var game))
            return (false, "No race lobby!");

        if (game.Started) return (false, "Race already running!");
        if (game.Racers.Count < 2) return (false, "Need at least 2 racers!");

        game.Started = true;
        var sb = new StringBuilder();
        sb.AppendLine("🏁 **RACE START!**\n");

        var results = new List<(ulong UserId, string Username, long Bet, int Score, RaceCar Car)>();

        foreach (var (userId, username, bet) in game.Racers)
        {
            var car = await GetOrCreateCar(userId);
            var score = car.Speed * _rng.Next(8, 13)
                + car.Handling * _rng.Next(5, 10)
                + car.Nitro * _rng.Next(3, 8)
                + _rng.Next(1, 50);
            results.Add((userId, username, bet, score, car));
        }

        results = results.OrderByDescending(r => r.Score).ToList();

        var positions = new[] { "🥇", "🥈", "🥉", "4th", "5th", "6th", "7th", "8th" };
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"{positions[Math.Min(i, positions.Length - 1)]} **{r.Username}** — Score: {r.Score} (S:{r.Car.Speed} H:{r.Car.Handling} N:{r.Car.Nitro})");
        }

        // Winner gets pot
        var winner = results[0];
        var pot = game.Racers.Sum(r => r.Bet);
        if (pot > 0)
        {
            await _cs.AddAsync(winner.UserId, pot, new TxData("race", "win"));
            sb.AppendLine($"\n🏆 **{winner.Username}** wins the pot of {pot} 🥠!");
        }
        else
        {
            await _cs.AddAsync(winner.UserId, 100, new TxData("race", "win"));
            sb.AppendLine($"\n🏆 **{winner.Username}** wins! +100 🥠");
        }

        // Update stats
        await using var ctx = _db.GetDbContext();
        foreach (var r in results)
        {
            await ctx.GetTable<RaceCar>()
                .Where(c => c.UserId == r.UserId)
                .UpdateAsync(c => new RaceCar { Races = c.Races + 1 });
        }
        await ctx.GetTable<RaceCar>()
            .Where(c => c.UserId == winner.UserId)
            .UpdateAsync(c => new RaceCar { Wins = c.Wins + 1 });

        ActiveRaces.TryRemove(channelId, out _);
        return (true, sb.ToString());
    }

    public async Task<RaceCar> GetGarageAsync(ulong userId)
    {
        return await GetOrCreateCar(userId);
    }

    public async Task<(bool Success, string Message)> UpgradeAsync(ulong userId, string stat)
    {
        var car = await GetOrCreateCar(userId);
        var cost = 500L;

        var removed = await _cs.RemoveAsync(userId, cost, new TxData("race", "upgrade"));
        if (!removed) return (false, $"Upgrade costs {cost} 🥠!");

        await using var ctx = _db.GetDbContext();

        switch (stat.ToLower())
        {
            case "speed":
                await ctx.GetTable<RaceCar>().Where(c => c.Id == car.Id)
                    .UpdateAsync(c => new RaceCar { Speed = car.Speed + 5 });
                return (true, $"Speed upgraded to {car.Speed + 5}!");
            case "handling":
                await ctx.GetTable<RaceCar>().Where(c => c.Id == car.Id)
                    .UpdateAsync(c => new RaceCar { Handling = car.Handling + 5 });
                return (true, $"Handling upgraded to {car.Handling + 5}!");
            case "nitro":
                await ctx.GetTable<RaceCar>().Where(c => c.Id == car.Id)
                    .UpdateAsync(c => new RaceCar { Nitro = car.Nitro + 5 });
                return (true, $"Nitro upgraded to {car.Nitro + 5}!");
            default:
                await _cs.AddAsync(userId, cost, new TxData("race", "upgrade-refund"));
                return (false, "Upgrade speed, handling, or nitro!");
        }
    }
}
