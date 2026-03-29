#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Social;

public sealed class TimeCapsuleService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public TimeCapsuleService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await DeliverCapsulesAsync();
            }
            catch { /* ignore */ }
        }
    }

    private async Task DeliverCapsulesAsync()
    {
        await using var ctx = _db.GetDbContext();
        var due = await ctx.GetTable<TimeCapsule>()
            .Where(x => !x.Delivered && x.DeliverAt <= DateTime.UtcNow)
            .ToListAsyncLinqToDB();

        foreach (var capsule in due)
        {
            try
            {
                var user = await _client.GetUserAsync(capsule.UserId);
                if (user is not null)
                {
                    var dm = await user.CreateDMChannelAsync();
                    await dm.SendMessageAsync(
                        $"\U0001F4E8 **Time Capsule from {capsule.DateAdded:MMM dd, yyyy}**\n\n{capsule.Message}");
                }
            }
            catch { /* user may have DMs disabled */ }

            await ctx.GetTable<TimeCapsule>()
                .Where(x => x.Id == capsule.Id)
                .Set(x => x.Delivered, true)
                .UpdateAsync();
        }
    }

    public async Task CreateCapsuleAsync(ulong guildId, ulong userId, string message, TimeSpan duration)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<TimeCapsule>()
            .InsertAsync(() => new TimeCapsule
            {
                GuildId = guildId,
                UserId = userId,
                Message = message,
                DeliverAt = DateTime.UtcNow.Add(duration),
                Delivered = false
            });
    }

    public async Task<List<TimeCapsule>> GetCapsulesAsync(ulong userId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<TimeCapsule>()
            .Where(x => x.UserId == userId && !x.Delivered)
            .OrderBy(x => x.DeliverAt)
            .ToListAsyncLinqToDB();
    }

    public static TimeSpan? ParseDuration(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim().ToLowerInvariant();

        if (input.EndsWith("d") && int.TryParse(input[..^1], out var days))
            return TimeSpan.FromDays(days);
        if (input.EndsWith("h") && int.TryParse(input[..^1], out var hours))
            return TimeSpan.FromHours(hours);
        if (input.EndsWith("m") && int.TryParse(input[..^1], out var mins))
            return TimeSpan.FromMinutes(mins);
        if (input.EndsWith("w") && int.TryParse(input[..^1], out var weeks))
            return TimeSpan.FromDays(weeks * 7);

        return null;
    }
}
