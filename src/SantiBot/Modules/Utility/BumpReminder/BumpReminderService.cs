using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Utility;

public sealed class BumpReminderService : INService, IReadyExecutor
{
    private const ulong DISBOARD_BOT_ID = 302050872383242240;
    private const string BUMP_DONE_TEXT = "Bump done";

    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly NonBlocking.ConcurrentDictionary<ulong, BumpReminderConfig> _configs = new();
    private readonly NonBlocking.ConcurrentDictionary<ulong, Timer> _timers = new();

    public BumpReminderService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var allConfigs = await ctx.GetTable<BumpReminderConfig>()
            .Where(x => x.Enabled)
            .ToListAsyncLinqToDB();

        foreach (var c in allConfigs)
        {
            _configs[c.GuildId] = c;

            // If there's a last bump, schedule a reminder for the remaining time
            if (c.LastBumpAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - c.LastBumpAt.Value;
                var remaining = TimeSpan.FromMinutes(c.IntervalMinutes) - elapsed;
                if (remaining <= TimeSpan.Zero)
                    remaining = TimeSpan.FromSeconds(5); // Fire soon

                ScheduleReminder(c.GuildId, remaining);
            }
        }

        _client.MessageReceived += OnMessageReceived;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (msg.Author.Id != DISBOARD_BOT_ID)
                    return;

                if (msg.Channel is not ITextChannel textChannel)
                    return;

                var guildId = textChannel.GuildId;

                if (!_configs.TryGetValue(guildId, out var config) || !config.Enabled)
                    return;

                // Check if the message or any embed contains "Bump done"
                var hasBumpDone = msg.Content.Contains(BUMP_DONE_TEXT, StringComparison.OrdinalIgnoreCase)
                    || msg.Embeds.Any(e =>
                        (e.Description?.Contains(BUMP_DONE_TEXT, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (e.Title?.Contains(BUMP_DONE_TEXT, StringComparison.OrdinalIgnoreCase) ?? false));

                if (!hasBumpDone)
                    return;

                // Record the bump time
                config.LastBumpAt = DateTime.UtcNow;
                await using var dbCtx = _db.GetDbContext();
                await dbCtx.GetTable<BumpReminderConfig>()
                    .Where(x => x.GuildId == guildId)
                    .Set(x => x.LastBumpAt, DateTime.UtcNow)
                    .UpdateAsync();

                // Schedule reminder
                ScheduleReminder(guildId, TimeSpan.FromMinutes(config.IntervalMinutes));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error in bump reminder message handler");
            }
        });

        return Task.CompletedTask;
    }

    private void ScheduleReminder(ulong guildId, TimeSpan delay)
    {
        // Cancel existing timer if any
        if (_timers.TryRemove(guildId, out var existing))
            existing.Dispose();

        var timer = new Timer(async _ =>
        {
            try
            {
                if (!_configs.TryGetValue(guildId, out var config) || !config.Enabled)
                    return;

                var guild = _client.GetGuild(guildId);
                if (guild is null)
                    return;

                var channel = guild.GetTextChannel(config.ChannelId);
                if (channel is null)
                    return;

                var roleMention = config.PingRoleId.HasValue
                    ? $"<@&{config.PingRoleId.Value}> "
                    : "";

                await channel.SendMessageAsync($"{roleMention}Time to bump the server! Use `/bump` with Disboard.");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error sending bump reminder for guild {GuildId}", guildId);
            }
        }, null, delay, Timeout.InfiniteTimeSpan);

        _timers[guildId] = timer;
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        await using var ctx = _db.GetDbContext();

        var config = await ctx.GetTable<BumpReminderConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (config is null)
        {
            config = new BumpReminderConfig
            {
                GuildId = guildId,
                ChannelId = channelId,
                Enabled = true
            };
            await ctx.GetTable<BumpReminderConfig>().InsertAsync(() => new BumpReminderConfig
            {
                GuildId = guildId,
                ChannelId = channelId,
                Enabled = true,
                IntervalMinutes = 120
            });
        }
        else
        {
            await ctx.GetTable<BumpReminderConfig>()
                .Where(x => x.GuildId == guildId)
                .Set(x => x.ChannelId, channelId)
                .Set(x => x.Enabled, true)
                .UpdateAsync();
            config.ChannelId = channelId;
            config.Enabled = true;
        }

        _configs[guildId] = config;
    }

    public async Task SetRoleAsync(ulong guildId, ulong roleId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<BumpReminderConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.PingRoleId, roleId)
            .UpdateAsync();

        if (_configs.TryGetValue(guildId, out var config))
            config.PingRoleId = roleId;
    }

    public async Task DisableAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<BumpReminderConfig>()
            .Where(x => x.GuildId == guildId)
            .Set(x => x.Enabled, false)
            .UpdateAsync();

        if (_configs.TryRemove(guildId, out _) && _timers.TryRemove(guildId, out var timer))
            timer.Dispose();
    }

    public BumpReminderConfig? GetConfig(ulong guildId)
        => _configs.TryGetValue(guildId, out var config) ? config : null;
}
