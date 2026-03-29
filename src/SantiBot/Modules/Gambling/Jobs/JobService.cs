#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.Jobs;

public sealed class JobService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public static readonly Dictionary<string, (long Pay, int CooldownHours, int WorksToUnlock)> JobTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Janitor"] = (50, 1, 0),
        ["Cook"] = (100, 2, 10),
        ["Developer"] = (200, 4, 25),
        ["CEO"] = (500, 8, 50),
    };

    public static readonly string[] JobOrder = ["Janitor", "Cook", "Developer", "CEO"];

    public JobService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<UserJob> GetUserJobAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserJob>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);
    }

    public async Task<(bool Success, string Message)> ApplyForJobAsync(ulong guildId, ulong userId, string jobName)
    {
        if (!JobTiers.TryGetValue(jobName, out var info))
            return (false, $"Unknown job. Available: {string.Join(", ", JobTiers.Keys)}");

        await using var ctx = _db.GetDbContext();
        var current = await ctx.GetTable<UserJob>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        // Check if they've done enough work to unlock this tier
        var totalWorked = current?.TimesWorked ?? 0;
        if (totalWorked < info.WorksToUnlock)
            return (false, $"You need {info.WorksToUnlock} total work sessions to unlock {jobName}. You have {totalWorked}.");

        if (current is null)
        {
            await ctx.GetTable<UserJob>().InsertAsync(() => new UserJob
            {
                GuildId = guildId,
                UserId = userId,
                JobName = jobName,
                TimesWorked = 0,
                LastWorked = DateTime.MinValue,
                DateAdded = DateTime.UtcNow
            });
        }
        else
        {
            await ctx.GetTable<UserJob>()
                .Where(x => x.Id == current.Id)
                .UpdateAsync(x => new UserJob { JobName = jobName });
        }

        return (true, $"You are now a **{jobName}**! Earn {info.Pay} 🥠 per shift with a {info.CooldownHours}h cooldown.");
    }

    public async Task<(bool Success, string Message)> WorkAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var job = await ctx.GetTable<UserJob>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        if (job is null)
            return (false, "You don't have a job! Use `.job apply Janitor` to get started.");

        if (!JobTiers.TryGetValue(job.JobName, out var info))
            return (false, "Invalid job data!");

        var cooldownEnd = job.LastWorked.AddHours(info.CooldownHours);
        if (DateTime.UtcNow < cooldownEnd)
        {
            var remaining = cooldownEnd - DateTime.UtcNow;
            return (false, $"You're on break! Come back in {remaining.Hours}h {remaining.Minutes}m.");
        }

        await _cs.AddAsync(userId, info.Pay, new TxData("job", "work"));

        await ctx.GetTable<UserJob>()
            .Where(x => x.Id == job.Id)
            .UpdateAsync(x => new UserJob
            {
                TimesWorked = job.TimesWorked + 1,
                LastWorked = DateTime.UtcNow
            });

        return (true, $"You worked as a **{job.JobName}** and earned {info.Pay} 🥠! (Total shifts: {job.TimesWorked + 1})");
    }

    public async Task<(bool Success, string Message)> QuitAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<UserJob>()
            .DeleteAsync(x => x.GuildId == guildId && x.UserId == userId);

        return deleted > 0 ? (true, "You quit your job!") : (false, "You don't have a job!");
    }
}
