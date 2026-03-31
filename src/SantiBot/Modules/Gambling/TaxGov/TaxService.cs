#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.TaxGov;

public sealed class TaxService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private Timer _electionTimer;

    public TaxService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public Task OnReadyAsync()
    {
        _electionTimer = new Timer(async _ => await ResolveElections(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private async Task ResolveElections()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var expired = await ctx.GetTable<TaxGovernment>()
                .Where(g => g.ElectionActive && g.ElectionEndsAt <= DateTime.UtcNow)
                .ToListAsyncLinqToDB();

            foreach (var gov in expired)
            {
                var votes = await ctx.GetTable<ElectionVote>()
                    .Where(v => v.GuildId == gov.GuildId)
                    .ToListAsyncLinqToDB();

                if (votes.Count > 0)
                {
                    var winner = votes.GroupBy(v => v.CandidateId)
                        .OrderByDescending(g => g.Count())
                        .First().Key;

                    await ctx.GetTable<TaxGovernment>()
                        .Where(g => g.Id == gov.Id)
                        .UpdateAsync(g => new TaxGovernment
                        {
                            ElectedOfficialId = winner,
                            ElectionActive = false
                        });
                }
                else
                {
                    await ctx.GetTable<TaxGovernment>()
                        .Where(g => g.Id == gov.Id)
                        .UpdateAsync(g => new TaxGovernment { ElectionActive = false });
                }

                await ctx.GetTable<ElectionVote>().DeleteAsync(v => v.GuildId == gov.GuildId);
            }
        }
        catch { /* timer safety */ }
    }

    private async Task<TaxGovernment> GetOrCreateGovAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gov = await ctx.GetTable<TaxGovernment>()
            .FirstOrDefaultAsyncLinqToDB(g => g.GuildId == guildId);

        if (gov is null)
        {
            await ctx.GetTable<TaxGovernment>().InsertAsync(() => new TaxGovernment
            {
                GuildId = guildId,
                TaxRate = 5,
                Treasury = 0,
                ElectedOfficialId = 0,
                ElectionActive = false,
                DateAdded = DateTime.UtcNow
            });
            gov = await ctx.GetTable<TaxGovernment>()
                .FirstOrDefaultAsyncLinqToDB(g => g.GuildId == guildId);
        }

        return gov;
    }

    public async Task<int> GetTaxRateAsync(ulong guildId)
    {
        var gov = await GetOrCreateGovAsync(guildId);
        return gov.TaxRate;
    }

    public async Task<(bool Success, string Message)> SetTaxRateAsync(ulong guildId, ulong userId, int rate)
    {
        if (rate is < 0 or > 50)
            return (false, "Tax rate must be between 0% and 50%!");

        var gov = await GetOrCreateGovAsync(guildId);
        if (gov.ElectedOfficialId != userId)
            return (false, "Only the elected official can set the tax rate!");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<TaxGovernment>()
            .Where(g => g.Id == gov.Id)
            .UpdateAsync(g => new TaxGovernment { TaxRate = rate });

        return (true, $"Tax rate set to **{rate}%**!");
    }

    public async Task<(bool Success, string Message)> StartElectionAsync(ulong guildId)
    {
        var gov = await GetOrCreateGovAsync(guildId);
        if (gov.ElectionActive)
            return (false, "An election is already in progress!");

        // Cooldown: only one election per 7 days
        if (gov.ElectionEndsAt != default && (DateTime.UtcNow - gov.ElectionEndsAt).TotalDays < 7)
            return (false, "Elections can only be held once per week!");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<TaxGovernment>()
            .Where(g => g.Id == gov.Id)
            .UpdateAsync(g => new TaxGovernment
            {
                ElectionActive = true,
                ElectionEndsAt = DateTime.UtcNow.AddHours(24)
            });

        return (true, "📊 **Election started!** Vote with `.election vote @user`. Ends in 24 hours!");
    }

    public async Task<(bool Success, string Message)> VoteAsync(ulong guildId, ulong voterId, ulong candidateId)
    {
        var gov = await GetOrCreateGovAsync(guildId);
        if (!gov.ElectionActive)
            return (false, "No active election!");

        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<ElectionVote>()
            .FirstOrDefaultAsyncLinqToDB(v => v.GuildId == guildId && v.VoterId == voterId);

        if (existing is not null)
        {
            await ctx.GetTable<ElectionVote>()
                .Where(v => v.Id == existing.Id)
                .UpdateAsync(v => new ElectionVote { CandidateId = candidateId });
            return (true, "Vote changed!");
        }

        await ctx.GetTable<ElectionVote>().InsertAsync(() => new ElectionVote
        {
            GuildId = guildId,
            VoterId = voterId,
            CandidateId = candidateId,
            DateAdded = DateTime.UtcNow
        });

        return (true, "Vote cast!");
    }

    public async Task<Dictionary<ulong, int>> GetElectionResultsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var votes = await ctx.GetTable<ElectionVote>()
            .Where(v => v.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return votes.GroupBy(v => v.CandidateId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<TaxGovernment> GetGovInfoAsync(ulong guildId)
    {
        return await GetOrCreateGovAsync(guildId);
    }

    /// <summary>
    /// Apply tax on a currency gain. Returns the tax amount deducted.
    /// Call this from gambling/economy services when awarding currency.
    /// </summary>
    public async Task<long> ApplyTaxAsync(ulong guildId, ulong userId, long amount)
    {
        var gov = await GetOrCreateGovAsync(guildId);
        if (gov.TaxRate <= 0 || amount <= 0)
            return 0;

        var tax = (long)(amount * gov.TaxRate / 100.0);
        if (tax <= 0)
            return 0;

        // Add tax to treasury
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<TaxGovernment>()
            .Where(g => g.Id == gov.Id)
            .UpdateAsync(g => new TaxGovernment { Treasury = gov.Treasury + tax });

        return tax;
    }

    /// <summary>
    /// Get current treasury balance.
    /// </summary>
    public async Task<long> GetTreasuryAsync(ulong guildId)
    {
        var gov = await GetOrCreateGovAsync(guildId);
        return gov.Treasury;
    }

    /// <summary>
    /// Withdraw from treasury. Only elected official can withdraw.
    /// </summary>
    public async Task<(bool Success, string Message)> WithdrawTreasuryAsync(ulong guildId, ulong userId, long amount)
    {
        if (amount <= 0)
            return (false, "Amount must be positive!");

        var gov = await GetOrCreateGovAsync(guildId);
        if (gov.ElectedOfficialId != userId)
            return (false, "Only the elected official can withdraw from the treasury!");

        if (amount > gov.Treasury)
            return (false, $"Treasury only has **{gov.Treasury}** 🥠!");

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<TaxGovernment>()
            .Where(g => g.Id == gov.Id)
            .UpdateAsync(g => new TaxGovernment { Treasury = gov.Treasury - amount });

        await _cs.AddAsync(userId, amount, new TxData("tax", "withdraw"));
        return (true, $"Withdrew **{amount}** 🥠 from the treasury. Remaining: **{gov.Treasury - amount}** 🥠");
    }
}
