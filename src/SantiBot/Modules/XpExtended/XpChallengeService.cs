#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.XpExtended;

public sealed class XpChallengeService : INService, IReadyExecutor
{
    private readonly DbService _db;

    private static readonly List<(string Type, string Desc, int Target, long Bonus)> _challengeTemplates = new()
    {
        ("messages", "Send {0} messages this week", 50, 500),
        ("messages", "Send {0} messages this week", 100, 1200),
        ("messages", "Send {0} messages this week", 200, 3000),
        ("reactions", "React to {0} messages this week", 30, 400),
        ("reactions", "React to {0} messages this week", 60, 900),
        ("voice", "Spend {0} minutes in voice this week", 60, 600),
        ("voice", "Spend {0} minutes in voice this week", 120, 1500),
    };

    public XpChallengeService(DbService db)
    {
        _db = db;
    }

    public async Task OnReadyAsync()
    {
        // rotate weekly challenges
        while (true)
        {
            var now = DateTime.UtcNow;
            var nextMonday = now.Date.AddDays(((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7);
            if (nextMonday <= now.Date) nextMonday = nextMonday.AddDays(7);
            await Task.Delay(nextMonday - now);

            try
            {
                await RotateChallengesAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task RotateChallengesAsync()
    {
        await using var ctx = _db.GetDbContext();

        // expire old challenges
        await ctx.GetTable<XpChallenge>()
            .Where(x => x.EndDate < DateTime.UtcNow)
            .DeleteAsync();
    }

    public async Task<List<XpChallenge>> GetActiveChallengesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var challenges = await ctx.GetTable<XpChallenge>()
            .Where(x => x.GuildId == guildId && x.EndDate > DateTime.UtcNow)
            .ToListAsyncLinqToDB();

        if (challenges.Count == 0)
        {
            // auto-create weekly challenges
            var rng = new Random();
            var selected = _challengeTemplates.OrderBy(_ => rng.Next()).Take(3).ToList();

            foreach (var tmpl in selected)
            {
                var id = await ctx.GetTable<XpChallenge>()
                    .InsertWithInt32IdentityAsync(() => new XpChallenge
                    {
                        GuildId = guildId,
                        ChallengeType = tmpl.Type,
                        Description = string.Format(tmpl.Desc, tmpl.Target),
                        TargetAmount = tmpl.Target,
                        BonusXp = tmpl.Bonus,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(7)
                    });

                challenges.Add(new XpChallenge
                {
                    Id = id,
                    GuildId = guildId,
                    ChallengeType = tmpl.Type,
                    Description = string.Format(tmpl.Desc, tmpl.Target),
                    TargetAmount = tmpl.Target,
                    BonusXp = tmpl.Bonus,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7)
                });
            }
        }

        return challenges;
    }

    public async Task IncrementProgressAsync(ulong guildId, ulong userId, string challengeType, int amount = 1)
    {
        await using var ctx = _db.GetDbContext();
        var challenges = await ctx.GetTable<XpChallenge>()
            .Where(x => x.GuildId == guildId && x.ChallengeType == challengeType && x.EndDate > DateTime.UtcNow)
            .ToListAsyncLinqToDB();

        foreach (var challenge in challenges)
        {
            var progress = await ctx.GetTable<XpChallengeProgress>()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.ChallengeId == challenge.Id)
                .FirstOrDefaultAsyncLinqToDB();

            if (progress is null)
            {
                await ctx.GetTable<XpChallengeProgress>()
                    .InsertAsync(() => new XpChallengeProgress
                    {
                        GuildId = guildId,
                        UserId = userId,
                        ChallengeId = challenge.Id,
                        CurrentAmount = amount,
                        Completed = amount >= challenge.TargetAmount,
                        Claimed = false
                    });
            }
            else if (!progress.Completed)
            {
                var newAmount = progress.CurrentAmount + amount;
                var completed = newAmount >= challenge.TargetAmount;

                await ctx.GetTable<XpChallengeProgress>()
                    .Where(x => x.Id == progress.Id)
                    .Set(x => x.CurrentAmount, newAmount)
                    .Set(x => x.Completed, completed)
                    .UpdateAsync();

                // auto-award XP on completion
                if (completed && !progress.Completed)
                {
                    await ctx.GetTable<UserXpStats>()
                        .Where(x => x.GuildId == guildId && x.UserId == userId)
                        .Set(x => x.Xp, x => x.Xp + challenge.BonusXp)
                        .UpdateAsync();

                    await ctx.GetTable<XpChallengeProgress>()
                        .Where(x => x.Id == progress.Id)
                        .Set(x => x.Claimed, true)
                        .UpdateAsync();
                }
            }
        }
    }

    public async Task<List<(XpChallenge Challenge, XpChallengeProgress Progress)>> GetUserProgressAsync(ulong guildId, ulong userId)
    {
        var challenges = await GetActiveChallengesAsync(guildId);
        var result = new List<(XpChallenge, XpChallengeProgress)>();

        await using var ctx = _db.GetDbContext();
        foreach (var c in challenges)
        {
            var progress = await ctx.GetTable<XpChallengeProgress>()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.ChallengeId == c.Id)
                .FirstOrDefaultAsyncLinqToDB()
                ?? new XpChallengeProgress { CurrentAmount = 0, Completed = false, Claimed = false };

            result.Add((c, progress));
        }

        return result;
    }
}
