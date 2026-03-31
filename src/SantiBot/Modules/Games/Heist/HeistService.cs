#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Games.Heist;

public sealed class HeistService(DbService _db, ICurrencyService _cs) : INService
{
    private static readonly SantiRandom _rng = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DateTime> _heistCooldowns = new();
    private const int HEIST_COOLDOWN_MINUTES = 10;

    private static readonly string[] _successNarratives =
    [
        "The crew slipped past the laser grid and cracked the vault wide open!",
        "A perfect disguise got the crew past security. The safe was emptied in seconds!",
        "The hacker disabled the alarms just in time. The crew grabbed everything!",
        "A daring rooftop entry led straight to the treasure room. Clean getaway!",
        "The distraction worked perfectly. The crew waltzed in and out unnoticed!",
        "Tunneling under the vault took hours, but the payoff was massive!",
    ];

    private static readonly string[] _failNarratives =
    [
        "An alarm tripped! The crew scattered and barely escaped empty-handed.",
        "The vault had a second lock nobody expected. The crew fled before cops arrived.",
        "A guard spotted the crew on camera. Everyone had to abandon the mission!",
        "The getaway driver panicked and left early. The crew lost everything running.",
        "Someone tipped off security. The heist was over before it began.",
        "The vault was empty -- someone beat the crew to it!",
    ];

    /// <summary>
    /// Start a new heist in a guild channel. Returns null if one is already active.
    /// </summary>
    public async Task<HeistSession> StartHeistAsync(ulong guildId, ulong channelId, ulong userId, long bet)
    {
        // Cooldown between heists
        if (_heistCooldowns.TryGetValue(guildId, out var lastHeist))
        {
            var cooldownEnd = lastHeist.AddMinutes(HEIST_COOLDOWN_MINUTES);
            if (DateTime.UtcNow < cooldownEnd)
                return null; // Caller handles null as "can't start"
        }

        await using var ctx = _db.GetDbContext();

        // Check for an active heist in this guild
        var existing = await ctx.GetTable<HeistSession>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && (x.Status == "Recruiting" || x.Status == "InProgress"));

        if (existing is not null)
            return null;

        // Take currency from initiator
        var taken = await _cs.RemoveAsync(userId, bet, new("heist", "bet", "Heist entry bet"));
        if (!taken)
            return null;

        var session = new HeistSession
        {
            GuildId = guildId,
            ChannelId = channelId,
            InitiatorUserId = userId,
            PotAmount = bet,
            ParticipantIds = userId.ToString(),
            Status = "Recruiting",
            StartedAt = DateTime.UtcNow,
        };

        ctx.Set<HeistSession>().Add(session);
        await ctx.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Join an active heist. Returns the updated session, or null if none exists / already joined.
    /// </summary>
    public async Task<(HeistSession session, string error)> JoinHeistAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        var session = await ctx.GetTable<HeistSession>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Status == "Recruiting");

        if (session is null)
            return (null, "no_heist");

        var ids = session.ParticipantIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ids.Contains(userId.ToString()))
            return (null, "already_joined");

        // Each joiner matches the per-person bet (pot / participant count)
        var perPersonBet = Math.Max(session.PotAmount / ids.Length, 50);
        var taken = await _cs.RemoveAsync(userId, perPersonBet, new("heist", "bet", "Heist join bet"));
        if (!taken)
            return (null, "not_enough");

        var newIds = session.ParticipantIds + "," + userId;
        var newPot = session.PotAmount + perPersonBet;

        await ctx.GetTable<HeistSession>()
            .Where(x => x.Id == session.Id)
            .UpdateAsync(_ => new HeistSession
            {
                ParticipantIds = newIds,
                PotAmount = newPot,
            });

        session.ParticipantIds = newIds;
        session.PotAmount = newPot;
        return (session, null);
    }

    /// <summary>
    /// Get the currently active heist for a guild, if any.
    /// </summary>
    public async Task<HeistSession> GetActiveHeistAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<HeistSession>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId
                && (x.Status == "Recruiting" || x.Status == "InProgress"));
    }

    /// <summary>
    /// Execute the heist and return results. Higher crew = better odds.
    /// Base success rate: 40%. Each extra crew member adds 10%, capped at 90%.
    /// </summary>
    public async Task<(bool success, string narrative, long payout, List<ulong> winners)> ExecuteHeistAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        var session = await ctx.GetTable<HeistSession>()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Status == "Recruiting");

        if (session is null)
            return (false, "No active heist found.", 0, new());

        // Require at least 2 crew members
        var crewIds = session.ParticipantIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (crewIds.Length < 2)
            return (false, "You need at least **2 crew members** to run a heist! Get someone to `.heist join`.", 0, new());

        // Mark as in progress
        await ctx.GetTable<HeistSession>()
            .Where(x => x.Id == session.Id)
            .UpdateAsync(_ => new HeistSession { Status = "InProgress" });

        var ids = session.ParticipantIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ulong.Parse)
            .ToList();

        var crewSize = ids.Count;
        var successChance = Math.Min(40 + (crewSize - 1) * 10, 90);
        var roll = _rng.Next(1, 101);
        var success = roll <= successChance;

        string narrative;
        long payout = 0;
        var winners = new List<ulong>();

        if (success)
        {
            narrative = _successNarratives[_rng.Next(0, _successNarratives.Length)];
            // Bonus multiplier: 1.5x to 3x based on crew size
            var multiplier = 1.5 + (_rng.Next(0, 16) * 0.1);
            payout = (long)(session.PotAmount * multiplier);
            var share = payout / crewSize;

            foreach (var uid in ids)
            {
                await _cs.AddAsync(uid, share, new("heist", "win", "Heist winnings"));
                winners.Add(uid);
            }
        }
        else
        {
            narrative = _failNarratives[_rng.Next(0, _failNarratives.Length)];
            // Everyone loses their bet -- currency was already removed
        }

        // Mark complete and record cooldown
        await ctx.GetTable<HeistSession>()
            .Where(x => x.Id == session.Id)
            .UpdateAsync(_ => new HeistSession { Status = "Complete" });

        _heistCooldowns[guildId] = DateTime.UtcNow;

        return (success, narrative, payout, winners);
    }
}
