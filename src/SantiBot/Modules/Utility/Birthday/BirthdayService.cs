using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class BirthdayService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly IBotCreds _creds;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;
    private readonly IBotStrings _strings;
    private readonly ILocalization _localization;

    public BirthdayService(
        DbService db,
        IBotCreds creds,
        DiscordSocketClient client,
        IMessageSenderService sender,
        IBotStrings strings,
        ILocalization localization)
    {
        _db = db;
        _creds = creds;
        _client = client;
        _sender = sender;
        _strings = strings;
        _localization = localization;
    }

    public async Task OnReadyAsync()
    {
        // Check for birthdays once a day
        var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        // Run an initial check on startup
        await CheckBirthdaysAsync();

        while (await timer.WaitForNextTickAsync())
        {
            // Only announce at the top of a new day (between 00:00 and 00:59 UTC)
            if (DateTime.UtcNow.Hour == 0)
                await CheckBirthdaysAsync();
        }
    }

    private async Task CheckBirthdaysAsync()
    {
        var today = DateTime.UtcNow;
        var month = today.Month;
        var day = today.Day;

        await using var ctx = _db.GetDbContext();

        // Get all birthdays for today across guilds on this shard
        var birthdays = await ctx
            .GetTable<UserBirthday>()
            .Where(x => x.Month == month && x.Day == day)
            .Where(x => Queries.GuildOnShard(x.GuildId, _creds.TotalShards, _client.ShardId))
            .ToArrayAsync();

        // Group by guild
        var grouped = birthdays.GroupBy(x => x.GuildId);

        foreach (var group in grouped)
        {
            var guildId = group.Key;
            var config = await ctx
                .GetTable<BirthdayConfig>()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

            if (config is null)
                continue;

            var guild = _client.GetGuild(guildId);
            if (guild is null)
                continue;

            // Grant birthday role if configured
            if (config.BirthdayRoleId is ulong roleId)
            {
                var role = guild.GetRole(roleId);
                if (role is not null)
                {
                    foreach (var birthday in group)
                    {
                        try
                        {
                            var user = guild.GetUser(birthday.UserId);
                            if (user is not null && !user.Roles.Any(r => r.Id == roleId))
                                await user.AddRoleAsync(role);
                        }
                        catch { }
                    }
                }
            }

            // Announce in channel if configured
            if (config.AnnouncementChannelId is ulong channelId)
            {
                var channel = guild.GetTextChannel(channelId);
                if (channel is not null)
                {
                    foreach (var birthday in group)
                    {
                        try
                        {
                            var msg = config.Message.Replace("{0}", $"<@{birthday.UserId}>");
                            await _sender.Response(channel).Text(msg).SendAsync();
                        }
                        catch { }
                    }
                }
            }
        }
    }

    public async Task SetBirthdayAsync(ulong guildId, ulong userId, int month, int day)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx
            .GetTable<UserBirthday>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);

        if (existing is not null)
        {
            await ctx.GetTable<UserBirthday>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new UserBirthday
                {
                    Month = month,
                    Day = day
                });
        }
        else
        {
            await ctx.GetTable<UserBirthday>()
                .InsertAsync(() => new UserBirthday
                {
                    GuildId = guildId,
                    UserId = userId,
                    Month = month,
                    Day = day
                });
        }
    }

    public async Task<UserBirthday?> GetBirthdayAsync(ulong guildId, ulong userId)
    {
        await using var ctx = _db.GetDbContext();

        return await ctx
            .GetTable<UserBirthday>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.UserId == userId);
    }

    public async Task<IReadOnlyCollection<UserBirthday>> GetUpcomingAsync(ulong guildId, int days = 30)
    {
        await using var ctx = _db.GetDbContext();

        var today = DateTime.UtcNow;
        var allBirthdays = await ctx
            .GetTable<UserBirthday>()
            .Where(x => x.GuildId == guildId)
            .ToListAsync();

        // Filter and sort by upcoming date
        var upcoming = allBirthdays
            .Select(b =>
            {
                var thisYear = new DateTime(today.Year, b.Month, b.Day);
                if (thisYear < today.Date)
                    thisYear = thisYear.AddYears(1);
                return (Birthday: b, NextDate: thisYear);
            })
            .Where(x => (x.NextDate - today.Date).TotalDays <= days)
            .OrderBy(x => x.NextDate)
            .Select(x => x.Birthday)
            .Take(10)
            .ToList();

        return upcoming;
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<BirthdayConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null)
        {
            await ctx.GetTable<BirthdayConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new BirthdayConfig
                {
                    AnnouncementChannelId = channelId
                });
        }
        else
        {
            await ctx.GetTable<BirthdayConfig>()
                .InsertAsync(() => new BirthdayConfig
                {
                    GuildId = guildId,
                    AnnouncementChannelId = channelId
                });
        }
    }

    public async Task SetRoleAsync(ulong guildId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx
            .GetTable<BirthdayConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is not null)
        {
            await ctx.GetTable<BirthdayConfig>()
                .Where(x => x.Id == config.Id)
                .UpdateAsync(x => new BirthdayConfig
                {
                    BirthdayRoleId = roleId
                });
        }
        else
        {
            await ctx.GetTable<BirthdayConfig>()
                .InsertAsync(() => new BirthdayConfig
                {
                    GuildId = guildId,
                    BirthdayRoleId = roleId
                });
        }
    }
}
