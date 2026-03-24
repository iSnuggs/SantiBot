#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class AutoMessageService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private Timer _timer;

    public AutoMessageService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public Task OnReadyAsync()
    {
        // Check for due messages every 30 seconds
        _timer = new Timer(async _ => await CheckScheduledMessagesAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        Log.Information("AutoMessage scheduler started");
        return Task.CompletedTask;
    }

    private async Task CheckScheduledMessagesAsync()
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var now = DateTime.UtcNow;

            // Get all active messages that are due
            var dueMessages = await uow.Set<ScheduledMessage>()
                .Where(m => m.IsActive && m.ScheduledAt <= now)
                .ToListAsyncEF();

            foreach (var msg in dueMessages)
            {
                try
                {
                    var guild = _client.GetGuild(msg.GuildId);
                    var channel = guild?.GetTextChannel(msg.ChannelId);
                    if (channel is null)
                    {
                        // Channel gone — deactivate
                        msg.IsActive = false;
                        continue;
                    }

                    await channel.SendMessageAsync(msg.Content);
                    msg.LastSentAt = now;

                    if (msg.IsRecurring && msg.Interval.HasValue)
                    {
                        // Schedule next occurrence
                        msg.ScheduledAt = now + msg.Interval.Value;
                    }
                    else
                    {
                        // One-time message — mark as done
                        msg.IsActive = false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to send scheduled message {Id}", msg.Id);
                    // Don't deactivate on transient failures
                }
            }

            await uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error checking scheduled messages");
        }
    }

    // ── Public API ──

    public async Task<ScheduledMessage> ScheduleOneTimeAsync(ulong guildId, ulong channelId, ulong creatorId,
        DateTime sendAt, string content)
    {
        await using var uow = _db.GetDbContext();
        var msg = new ScheduledMessage
        {
            GuildId = guildId,
            ChannelId = channelId,
            CreatorUserId = creatorId,
            Content = content,
            ScheduledAt = sendAt,
            IsRecurring = false,
            IsActive = true,
        };

        uow.Set<ScheduledMessage>().Add(msg);
        await uow.SaveChangesAsync();
        return msg;
    }

    public async Task<ScheduledMessage> ScheduleRecurringAsync(ulong guildId, ulong channelId, ulong creatorId,
        TimeSpan interval, string content, DateTime? startAt = null)
    {
        await using var uow = _db.GetDbContext();
        var msg = new ScheduledMessage
        {
            GuildId = guildId,
            ChannelId = channelId,
            CreatorUserId = creatorId,
            Content = content,
            ScheduledAt = startAt ?? DateTime.UtcNow + interval,
            Interval = interval,
            IsRecurring = true,
            IsActive = true,
        };

        uow.Set<ScheduledMessage>().Add(msg);
        await uow.SaveChangesAsync();
        return msg;
    }

    public async Task<bool> CancelMessageAsync(ulong guildId, int messageId)
    {
        await using var uow = _db.GetDbContext();
        var msg = await uow.Set<ScheduledMessage>()
            .FirstOrDefaultAsyncEF(m => m.Id == messageId && m.GuildId == guildId);

        if (msg is null)
            return false;

        msg.IsActive = false;
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScheduledMessage>> GetActiveMessagesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ScheduledMessage>()
            .AsNoTracking()
            .Where(m => m.GuildId == guildId && m.IsActive)
            .OrderBy(m => m.ScheduledAt)
            .ToListAsyncEF();
    }
}
