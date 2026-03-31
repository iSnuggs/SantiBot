#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.Loans;

public sealed class LoanService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;
    private Timer _interestTimer;

    private const double DAILY_INTEREST_RATE = 0.10; // 10% per day

    public LoanService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public Task OnReadyAsync()
    {
        _interestTimer = new Timer(async _ => await ApplyInterest(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private const int MAX_LOAN_DAYS = 14; // Auto-default after 14 days

    private async Task ApplyInterest()
    {
        try
        {
            await using var ctx = _db.GetDbContext();
            var activeLoans = await ctx.GetTable<UserLoan>()
                .Where(l => l.IsActive)
                .ToListAsyncLinqToDB();

            foreach (var loan in activeLoans)
            {
                // Auto-default after 14 days — currency seized from wallet, credit score destroyed
                var daysSinceTaken = (DateTime.UtcNow - loan.TakenAt).TotalDays;
                if (daysSinceTaken > MAX_LOAN_DAYS)
                {
                    // Try to seize whatever currency they have
                    await _cs.RemoveAsync(loan.UserId, loan.AmountOwed,
                        new TxData("loan", "default-seize", "Loan defaulted — currency seized"));

                    // Mark loan as defaulted
                    await ctx.GetTable<UserLoan>()
                        .Where(l => l.Id == loan.Id)
                        .UpdateAsync(l => new UserLoan { IsActive = false, AmountOwed = 0 });

                    // Record as late repayment (tanks credit score)
                    await ctx.GetTable<LoanHistory>().InsertAsync(() => new LoanHistory
                    {
                        GuildId = loan.GuildId,
                        UserId = loan.UserId,
                        Amount = loan.Principal,
                        RepaidOnTime = false,
                        DateAdded = DateTime.UtcNow
                    });
                    continue;
                }

                var hoursSinceLastInterest = (DateTime.UtcNow - loan.LastInterestApplied).TotalHours;
                if (hoursSinceLastInterest < 24) continue;

                var interest = (long)(loan.AmountOwed * DAILY_INTEREST_RATE);
                await ctx.GetTable<UserLoan>()
                    .Where(l => l.Id == loan.Id)
                    .UpdateAsync(l => new UserLoan
                    {
                        AmountOwed = loan.AmountOwed + interest,
                        LastInterestApplied = DateTime.UtcNow
                    });
            }
        }
        catch { /* timer safety */ }
    }

    private long GetMaxLoan(int creditScore) => creditScore switch
    {
        >= 800 => 50000,
        >= 700 => 25000,
        >= 600 => 10000,
        >= 500 => 5000,
        _ => 1000
    };

    public async Task<(bool Success, string Message)> TakeLoanAsync(ulong guildId, ulong userId, long amount)
    {
        if (amount < 100)
            return (false, "Minimum loan is 100 🥠!");

        await using var ctx = _db.GetDbContext();

        // Check for existing active loan
        var existing = await ctx.GetTable<UserLoan>()
            .FirstOrDefaultAsyncLinqToDB(l => l.GuildId == guildId && l.UserId == userId && l.IsActive);

        if (existing is not null)
            return (false, "You already have an active loan! Repay it first.");

        // Get or create credit score
        var history = await ctx.GetTable<LoanHistory>()
            .Where(h => h.GuildId == guildId && h.UserId == userId)
            .ToListAsyncLinqToDB();

        var creditScore = 500 + history.Count(h => h.RepaidOnTime) * 50 - history.Count(h => !h.RepaidOnTime) * 100;
        creditScore = Math.Clamp(creditScore, 100, 1000);

        var maxLoan = GetMaxLoan(creditScore);
        if (amount > maxLoan)
            return (false, $"Your credit score ({creditScore}) only allows loans up to {maxLoan} 🥠!");

        await ctx.GetTable<UserLoan>().InsertAsync(() => new UserLoan
        {
            GuildId = guildId,
            UserId = userId,
            Principal = amount,
            AmountOwed = amount,
            CreditScore = creditScore,
            TakenAt = DateTime.UtcNow,
            LastInterestApplied = DateTime.UtcNow,
            IsActive = true,
            DateAdded = DateTime.UtcNow
        });

        await _cs.AddAsync(userId, amount, new TxData("loan", "take"));

        return (true, $"Loan of {amount} 🥠 approved! Credit score: {creditScore}. Interest: 10%/day. Repay ASAP!");
    }

    public async Task<(bool Success, string Message)> RepayLoanAsync(ulong guildId, ulong userId, long? amount)
    {
        if (amount.HasValue && amount.Value <= 0)
            return (false, "Repayment amount must be positive!");

        await using var ctx = _db.GetDbContext();
        var loan = await ctx.GetTable<UserLoan>()
            .FirstOrDefaultAsyncLinqToDB(l => l.GuildId == guildId && l.UserId == userId && l.IsActive);

        if (loan is null)
            return (false, "You don't have an active loan!");

        var repayAmount = amount ?? loan.AmountOwed;
        if (repayAmount > loan.AmountOwed)
            repayAmount = loan.AmountOwed;

        var removed = await _cs.RemoveAsync(userId, repayAmount, new TxData("loan", "repay"));
        if (!removed)
            return (false, $"You don't have {repayAmount} 🥠!");

        var remaining = loan.AmountOwed - repayAmount;

        if (remaining <= 0)
        {
            await ctx.GetTable<UserLoan>()
                .Where(l => l.Id == loan.Id)
                .UpdateAsync(l => new UserLoan { AmountOwed = 0, IsActive = false });

            var onTime = (DateTime.UtcNow - loan.TakenAt).TotalDays <= 7;
            await ctx.GetTable<LoanHistory>().InsertAsync(() => new LoanHistory
            {
                GuildId = guildId,
                UserId = userId,
                Amount = loan.Principal,
                RepaidOnTime = onTime,
                DateAdded = DateTime.UtcNow
            });

            return (true, $"Loan fully repaid! {(onTime ? "Paid on time — credit score improved!" : "Paid late — credit score may decrease.")}");
        }

        await ctx.GetTable<UserLoan>()
            .Where(l => l.Id == loan.Id)
            .UpdateAsync(l => new UserLoan { AmountOwed = remaining });

        return (true, $"Repaid {repayAmount} 🥠. Remaining: {remaining} 🥠.");
    }

    public async Task<UserLoan> GetLoanStatusAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserLoan>()
            .FirstOrDefaultAsyncLinqToDB(l => l.GuildId == guildId && l.UserId == userId && l.IsActive);
    }

    public async Task<List<LoanHistory>> GetHistoryAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<LoanHistory>()
            .Where(h => h.GuildId == guildId && h.UserId == userId)
            .OrderByDescending(h => h.DateAdded)
            .Take(10)
            .ToListAsyncLinqToDB();
    }
}
