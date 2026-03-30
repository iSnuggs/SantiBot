#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class TicketService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    private readonly ConcurrentDictionary<ulong, TicketConfig> _configs = new();

    public TicketService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var configs = await uow.Set<TicketConfig>()
            .AsNoTracking()
            .Where(c => c.Enabled)
            .ToListAsyncEF();

        foreach (var config in configs)
            _configs[config.GuildId] = config;

        // Listen for button clicks on ticket panels
        _client.InteractionCreated += OnInteractionCreated;

        Log.Information("Ticket system loaded {Count} guild configs", configs.Count);
    }

    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component)
            return;

        if (component.Data.CustomId == "ticket:create")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await component.DeferAsync(ephemeral: true);
                    var guild = (component.Channel as SocketGuildChannel)?.Guild;
                    if (guild is null) return;

                    var result = await CreateTicketAsync(guild.Id, component.User.Id);
                    if (result.success)
                        await component.FollowupAsync($"Ticket created! Head to <#{result.channelId}>", ephemeral: true);
                    else
                        await component.FollowupAsync(result.error, ephemeral: true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Ticket button handler failed");
                }
            });
        }
        else if (component.Data.CustomId == "ticket:close")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await component.DeferAsync(ephemeral: true);
                    var guild = (component.Channel as SocketGuildChannel)?.Guild;
                    if (guild is null) return;

                    // Permission check: only ticket creator, staff role, or admins can close
                    var guildUser = guild.GetUser(component.User.Id);
                    var config = _configs.TryGetValue(guild.Id, out var cfg) ? cfg : null;
                    var isStaff = guildUser is not null &&
                        (guildUser.GuildPermissions.Administrator ||
                         guildUser.GuildPermissions.ManageChannels ||
                         (config?.SupportRoleId is not null && guildUser.Roles.Any(r => r.Id == config.SupportRoleId.Value)));

                    // Check if they're the ticket creator
                    await using var checkCtx = _db.GetDbContext();
                    var ticket = await checkCtx.Set<Ticket>()
                        .FirstOrDefaultAsyncEF(t => t.GuildId == guild.Id && t.ChannelId == component.Channel.Id && t.Status != TicketStatus.Closed);
                    var isCreator = ticket is not null && ticket.CreatorUserId == component.User.Id;

                    if (!isStaff && !isCreator)
                    {
                        await component.FollowupAsync("Only staff or the ticket creator can close tickets.", ephemeral: true);
                        return;
                    }

                    var success = await CloseTicketByChannelAsync(guild.Id, component.Channel.Id, component.User.Id);
                    if (success)
                        await component.FollowupAsync("Ticket will be closed shortly.", ephemeral: true);
                    else
                        await component.FollowupAsync("This doesn't appear to be a ticket channel.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Ticket close button handler failed");
                }
            });
        }
        else if (component.Data.CustomId == "ticket:claim")
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await component.DeferAsync(ephemeral: true);
                    var guild = (component.Channel as SocketGuildChannel)?.Guild;
                    if (guild is null) return;

                    // Permission check: only staff or admins can claim tickets
                    var claimUser = guild.GetUser(component.User.Id);
                    var claimConfig = _configs.TryGetValue(guild.Id, out var claimCfg) ? claimCfg : null;
                    var canClaim = claimUser is not null &&
                        (claimUser.GuildPermissions.Administrator ||
                         claimUser.GuildPermissions.ManageChannels ||
                         (claimConfig?.SupportRoleId is not null && claimUser.Roles.Any(r => r.Id == claimConfig.SupportRoleId.Value)));

                    if (!canClaim)
                    {
                        await component.FollowupAsync("Only staff members can claim tickets.", ephemeral: true);
                        return;
                    }

                    var success = await ClaimTicketByChannelAsync(guild.Id, component.Channel.Id, component.User.Id);
                    if (success)
                        await component.FollowupAsync($"Ticket claimed by {component.User.Mention}!", ephemeral: false);
                    else
                        await component.FollowupAsync("Could not claim this ticket.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Ticket claim button handler failed");
                }
            });
        }
    }

    // ── Ticket Creation ──

    public async Task<(bool success, ulong channelId, string error)> CreateTicketAsync(ulong guildId, ulong userId, string topic = null)
    {
        if (!_configs.TryGetValue(guildId, out var config) || !config.Enabled)
            return (false, 0, "Ticket system is not enabled on this server.");

        var guild = _client.GetGuild(guildId);
        if (guild is null)
            return (false, 0, "Guild not found.");

        // Check max tickets per user
        if (config.MaxTicketsPerUser > 0)
        {
            await using var checkUow = _db.GetDbContext();
            var openCount = await checkUow.Set<Ticket>()
                .CountAsyncEF(t => t.GuildId == guildId && t.CreatorUserId == userId && t.Status != TicketStatus.Closed);

            if (openCount >= config.MaxTicketsPerUser)
                return (false, 0, $"You already have {openCount} open ticket(s). Max is {config.MaxTicketsPerUser}.");
        }

        // Get next ticket number
        await using var uow = _db.GetDbContext();
        var maxNum = await uow.Set<Ticket>()
            .Where(t => t.GuildId == guildId)
            .OrderByDescending(t => t.TicketNumber)
            .Select(t => t.TicketNumber)
            .FirstOrDefaultAsyncEF();

        var ticketNumber = maxNum + 1;

        // Create the ticket channel
        var user = guild.GetUser(userId);
        var channelName = $"ticket-{ticketNumber:D4}";

        try
        {
            var category = config.CategoryId.HasValue ? guild.GetCategoryChannel(config.CategoryId.Value) : null;

            var channel = await guild.CreateTextChannelAsync(channelName, props =>
            {
                if (category is not null)
                    props.CategoryId = category.Id;
                props.Topic = topic ?? $"Ticket #{ticketNumber} by {user?.DisplayName ?? userId.ToString()}";
            });

            // Set permissions — deny everyone, allow ticket creator and support role
            var everyone = guild.EveryoneRole;
            await channel.AddPermissionOverwriteAsync(everyone, new OverwritePermissions(viewChannel: PermValue.Deny));

            if (user is not null)
                await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(
                    viewChannel: PermValue.Allow, sendMessages: PermValue.Allow,
                    attachFiles: PermValue.Allow, embedLinks: PermValue.Allow));

            if (config.SupportRoleId.HasValue)
            {
                var supportRole = guild.GetRole(config.SupportRoleId.Value);
                if (supportRole is not null)
                    await channel.AddPermissionOverwriteAsync(supportRole, new OverwritePermissions(
                        viewChannel: PermValue.Allow, sendMessages: PermValue.Allow,
                        manageMessages: PermValue.Allow));
            }

            // Save ticket to DB
            var ticket = new Ticket
            {
                GuildId = guildId,
                TicketNumber = ticketNumber,
                CreatorUserId = userId,
                ChannelId = channel.Id,
                Topic = topic,
                Status = TicketStatus.Open,
            };

            uow.Set<Ticket>().Add(ticket);
            await uow.SaveChangesAsync();

            // Send welcome message with close/claim buttons
            var welcomeText = (config.WelcomeMessage ?? "Hey {user}, a staff member will be with you shortly.")
                .Replace("{user}", user?.Mention ?? $"<@{userId}>")
                .Replace("{server}", guild.Name)
                .Replace("{ticket}", $"#{ticketNumber}");

            var embed = _sender.CreateEmbed(guildId)
                .WithTitle($"Ticket #{ticketNumber}")
                .WithDescription(welcomeText)
                .WithOkColor();

            if (!string.IsNullOrEmpty(topic))
                embed.AddField("Topic", topic);

            var components = new ComponentBuilder()
                .WithButton("Claim", "ticket:claim", ButtonStyle.Primary, new Emoji("🙋"))
                .WithButton("Close", "ticket:close", ButtonStyle.Danger, new Emoji("🔒"))
                .Build();

            await channel.SendMessageAsync(embed: embed.Build(), components: components);

            return (true, channel.Id, null);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create ticket channel");
            return (false, 0, "Failed to create ticket channel. Check bot permissions.");
        }
    }

    // ── Ticket Actions ──

    public async Task<bool> CloseTicketByChannelAsync(ulong guildId, ulong channelId, ulong closedByUserId)
    {
        await using var uow = _db.GetDbContext();
        var ticket = await uow.Set<Ticket>()
            .FirstOrDefaultAsyncEF(t => t.GuildId == guildId && t.ChannelId == channelId && t.Status != TicketStatus.Closed);

        if (ticket is null)
            return false;

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.ClosedByUserId = closedByUserId;
        await uow.SaveChangesAsync();

        // Collect transcript before deleting the channel
        string transcript = null;
        try
        {
            var guild = _client.GetGuild(guildId);
            var ticketChannel = guild?.GetTextChannel(channelId);
            if (ticketChannel is not null)
            {
                var messages = await ticketChannel.GetMessagesAsync(500).FlattenAsync();
                var ordered = messages.OrderBy(m => m.Timestamp).ToList();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== Ticket #{ticket.TicketNumber} Transcript ===");
                sb.AppendLine($"Opened: {ticket.CreatedAt:yyyy-MM-dd HH:mm UTC}");
                sb.AppendLine($"Closed: {ticket.ClosedAt:yyyy-MM-dd HH:mm UTC}");
                sb.AppendLine($"Opened by: {ticket.CreatorUserId}");
                sb.AppendLine($"Closed by: {closedByUserId}");
                sb.AppendLine(new string('─', 50));
                foreach (var msg in ordered)
                {
                    var time = msg.Timestamp.UtcDateTime.ToString("HH:mm");
                    var author = msg.Author?.Username ?? "Unknown";
                    sb.AppendLine($"[{time}] {author}: {msg.Content}");
                    if (msg.Attachments.Count > 0)
                        sb.AppendLine($"  📎 {string.Join(", ", msg.Attachments.Select(a => a.Url))}");
                }
                transcript = sb.ToString();
            }
        }
        catch (Exception ex) { Log.Warning(ex, "Failed to collect ticket transcript"); }

        // Log to transcript channel
        if (_configs.TryGetValue(guildId, out var config) && config.LogChannelId.HasValue)
        {
            try
            {
                var guild = _client.GetGuild(guildId);
                var logChannel = guild?.GetTextChannel(config.LogChannelId.Value);
                if (logChannel is not null)
                {
                    var embed = _sender.CreateEmbed(guildId)
                        .WithTitle($"Ticket #{ticket.TicketNumber} Closed")
                        .AddField("Opened by", $"<@{ticket.CreatorUserId}>", true)
                        .AddField("Closed by", $"<@{closedByUserId}>", true)
                        .AddField("Duration", (ticket.ClosedAt.Value - ticket.CreatedAt).Humanize(), true)
                        .WithColor(new Color(0xFF0000))
                        .WithTimestamp(DateTime.UtcNow);

                    if (ticket.ClaimedByUserId.HasValue)
                        embed.AddField("Claimed by", $"<@{ticket.ClaimedByUserId}>", true);

                    await logChannel.SendMessageAsync(embed: embed.Build());

                    // Send transcript as a text file attachment
                    if (transcript is not null)
                    {
                        using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(transcript));
                        await logChannel.SendFileAsync(stream, $"ticket-{ticket.TicketNumber}-transcript.txt",
                            $"Transcript for ticket #{ticket.TicketNumber}");
                    }
                }
            }
            catch { }
        }

        // Delete the channel after a short delay
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000);
                var guild = _client.GetGuild(guildId);
                var channel = guild?.GetTextChannel(channelId);
                if (channel is not null)
                    await channel.DeleteAsync();
            }
            catch { }
        });

        return true;
    }

    public async Task<bool> ClaimTicketByChannelAsync(ulong guildId, ulong channelId, ulong modUserId)
    {
        await using var uow = _db.GetDbContext();
        var ticket = await uow.Set<Ticket>()
            .FirstOrDefaultAsyncEF(t => t.GuildId == guildId && t.ChannelId == channelId && t.Status == TicketStatus.Open);

        if (ticket is null)
            return false;

        ticket.Status = TicketStatus.Claimed;
        ticket.ClaimedByUserId = modUserId;
        await uow.SaveChangesAsync();

        return true;
    }

    public async Task<List<Ticket>> GetOpenTicketsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<Ticket>()
            .AsNoTracking()
            .Where(t => t.GuildId == guildId && t.Status != TicketStatus.Closed)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsyncEF();
    }

    // ── Configuration ──

    public async Task<TicketConfig> GetOrCreateConfigAsync(ulong guildId)
    {
        if (_configs.TryGetValue(guildId, out var cached))
            return cached;

        await using var uow = _db.GetDbContext();
        var config = await uow.Set<TicketConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new TicketConfig { GuildId = guildId };
            uow.Set<TicketConfig>().Add(config);
            await uow.SaveChangesAsync();
        }

        _configs[guildId] = config;
        return config;
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var uow = _db.GetDbContext();
        var config = await uow.Set<TicketConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new TicketConfig { GuildId = guildId, Enabled = enabled };
            uow.Set<TicketConfig>().Add(config);
        }
        else
        {
            config.Enabled = enabled;
        }

        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetCategoryAsync(ulong guildId, ulong? categoryId)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<TicketConfig>().Attach(config);
        config.CategoryId = categoryId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetLogChannelAsync(ulong guildId, ulong? channelId)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<TicketConfig>().Attach(config);
        config.LogChannelId = channelId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetSupportRoleAsync(ulong guildId, ulong? roleId)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<TicketConfig>().Attach(config);
        config.SupportRoleId = roleId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetWelcomeMessageAsync(ulong guildId, string message)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<TicketConfig>().Attach(config);
        config.WelcomeMessage = message;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetMaxTicketsAsync(ulong guildId, int max)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<TicketConfig>().Attach(config);
        config.MaxTicketsPerUser = max;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    /// <summary>Sends a panel with a "Create Ticket" button to the specified channel.</summary>
    public async Task<bool> SendPanelAsync(ulong guildId, ITextChannel channel)
    {
        var config = await GetOrCreateConfigAsync(guildId);

        var embed = _sender.CreateEmbed(guildId)
            .WithTitle("🎫 Support Tickets")
            .WithDescription("Click the button below to create a support ticket.\nA private channel will be created for you and the staff team.")
            .WithOkColor();

        var components = new ComponentBuilder()
            .WithButton("Create Ticket", "ticket:create", ButtonStyle.Success, new Emoji("🎫"))
            .Build();

        var msg = await channel.SendMessageAsync(embed: embed.Build(), components: components);

        // Save panel info
        await using var uow = _db.GetDbContext();
        var dbConfig = await uow.Set<TicketConfig>().FirstOrDefaultAsyncEF(c => c.GuildId == guildId);
        if (dbConfig is not null)
        {
            dbConfig.PanelChannelId = channel.Id;
            dbConfig.PanelMessageId = msg.Id;
            await uow.SaveChangesAsync();
        }

        return true;
    }
}

// Extension for humanizing TimeSpan
internal static class TimeSpanExtensions
{
    public static string Humanize(this TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{(int)ts.TotalMinutes}m";
    }
}
