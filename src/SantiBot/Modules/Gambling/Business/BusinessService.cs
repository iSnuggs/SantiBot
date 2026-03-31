#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using SantiBot.Services.Currency;

namespace SantiBot.Modules.Gambling.Business;

public sealed class BusinessService : INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public static readonly Dictionary<string, (long Cost, long BaseRevenue, int MaxEmployees)> BusinessTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Restaurant"] = (5000, 100, 5),
        ["Shop"] = (3000, 60, 4),
        ["Farm"] = (2000, 40, 6),
        ["Factory"] = (10000, 200, 8),
    };

    public BusinessService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<(bool Success, string Message)> CreateBusinessAsync(ulong guildId, ulong ownerId, string name, string type)
    {
        if (!BusinessTypes.TryGetValue(type, out var info))
            return (false, $"Unknown type. Available: {string.Join(", ", BusinessTypes.Keys)}");

        await using var ctx = _db.GetDbContext();
        var existing = await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.GuildId == guildId && b.OwnerId == ownerId);

        if (existing is not null)
            return (false, "You already own a business! Sell it first.");

        var removed = await _cs.RemoveAsync(ownerId, info.Cost, new TxData("business", "create"));
        if (!removed)
            return (false, $"You need {info.Cost} 🥠 to start a {type}!");

        await ctx.GetTable<UserBusiness>().InsertAsync(() => new UserBusiness
        {
            GuildId = guildId,
            OwnerId = ownerId,
            Name = name,
            BusinessType = type,
            Revenue = 0,
            LastCollected = DateTime.UtcNow,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"**{name}** ({type}) is now open for business! Cost: {info.Cost} 🥠. Hire employees with `.biz hire`.");
    }

    public async Task<(bool Success, string Message)> HireAsync(ulong guildId, ulong ownerId, ulong employeeId)
    {
        if (ownerId == employeeId)
            return (false, "You can't hire yourself!");

        await using var ctx = _db.GetDbContext();
        var biz = await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.GuildId == guildId && b.OwnerId == ownerId);

        if (biz is null)
            return (false, "You don't own a business!");

        if (!BusinessTypes.TryGetValue(biz.BusinessType, out var info))
            return (false, "Invalid business type!");

        var empCount = await ctx.GetTable<BusinessEmployee>()
            .CountAsyncLinqToDB(e => e.BusinessId == biz.Id);

        if (empCount >= info.MaxEmployees)
            return (false, $"Max employees ({info.MaxEmployees}) reached!");

        var alreadyHired = await ctx.GetTable<BusinessEmployee>()
            .AnyAsyncLinqToDB(e => e.BusinessId == biz.Id && e.UserId == employeeId);

        if (alreadyHired)
            return (false, "That user is already an employee!");

        await ctx.GetTable<BusinessEmployee>().InsertAsync(() => new BusinessEmployee
        {
            GuildId = guildId,
            BusinessId = biz.Id,
            UserId = employeeId,
            HiredAt = DateTime.UtcNow,
            LastWorked = DateTime.MinValue,
            DateAdded = DateTime.UtcNow
        });

        return (true, $"<@{employeeId}> has been hired at **{biz.Name}**!");
    }

    public async Task<(bool Success, string Message)> FireAsync(ulong guildId, ulong ownerId, ulong employeeId)
    {
        await using var ctx = _db.GetDbContext();
        var biz = await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.GuildId == guildId && b.OwnerId == ownerId);

        if (biz is null)
            return (false, "You don't own a business!");

        var deleted = await ctx.GetTable<BusinessEmployee>()
            .DeleteAsync(e => e.BusinessId == biz.Id && e.UserId == employeeId);

        return deleted > 0 ? (true, $"<@{employeeId}> has been fired!") : (false, "That user isn't an employee!");
    }

    public async Task<(bool Success, string Message)> WorkAtBusinessAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        var emp = await ctx.GetTable<BusinessEmployee>()
            .FirstOrDefaultAsyncLinqToDB(e => e.GuildId == guildId && e.UserId == userId);

        if (emp is null)
            return (false, "You don't work anywhere! Ask a business owner to hire you.");

        var cooldown = emp.LastWorked.AddHours(2);
        if (DateTime.UtcNow < cooldown)
        {
            var remaining = cooldown - DateTime.UtcNow;
            return (false, $"On break! Come back in {remaining.Hours}h {remaining.Minutes}m.");
        }

        var biz = await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.Id == emp.BusinessId);

        if (biz is null)
            return (false, "Your business no longer exists!");

        if (!BusinessTypes.TryGetValue(biz.BusinessType, out var info))
            return (false, "Invalid business!");

        var salary = info.BaseRevenue / 2; // Employee gets half
        var profit = info.BaseRevenue / 2; // Owner gets other half

        await _cs.AddAsync(userId, salary, new TxData("business", "salary"));

        await ctx.GetTable<UserBusiness>()
            .Where(b => b.Id == biz.Id)
            .UpdateAsync(b => new UserBusiness { Revenue = biz.Revenue + profit });

        await ctx.GetTable<BusinessEmployee>()
            .Where(e => e.Id == emp.Id)
            .UpdateAsync(e => new BusinessEmployee { LastWorked = DateTime.UtcNow });

        return (true, $"You worked at **{biz.Name}** and earned {salary} 🥠!");
    }

    public async Task<(bool Success, string Message, long Amount)> CollectRevenueAsync(ulong guildId, ulong ownerId)
    {
        await using var ctx = _db.GetDbContext();
        var biz = await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.GuildId == guildId && b.OwnerId == ownerId);

        if (biz is null)
            return (false, "You don't own a business!", 0);

        if (biz.Revenue <= 0)
            return (false, "No revenue to collect yet!", 0);

        var revenue = biz.Revenue;
        await ctx.GetTable<UserBusiness>()
            .Where(b => b.Id == biz.Id)
            .UpdateAsync(b => new UserBusiness { Revenue = 0 });

        await _cs.AddAsync(ownerId, revenue, new TxData("business", "collect"));
        return (true, $"Collected **{revenue}** 🥠 from **{biz.Name}**!", revenue);
    }

    public async Task<UserBusiness> GetBusinessInfoAsync(ulong guildId, ulong ownerId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserBusiness>()
            .FirstOrDefaultAsyncLinqToDB(b => b.GuildId == guildId && b.OwnerId == ownerId);
    }

    public async Task<int> GetEmployeeCountAsync(int bizId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<BusinessEmployee>()
            .CountAsyncLinqToDB(e => e.BusinessId == bizId);
    }

    public async Task<List<UserBusiness>> ListBusinessesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<UserBusiness>()
            .Where(b => b.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }
}
