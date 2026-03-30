#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class ModMailService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    // Guild configs cache
    private readonly ConcurrentDictionary<ulong, ModMailConfig> _configs = new();

    // Active thread lookup: channelId → thread
    private readonly ConcurrentDictionary<ulong, ModMailThread> _activeByChannel = new();

    // Active thread lookup: (guildId, userId) → thread (for DM routing)
    private readonly ConcurrentDictionary<(ulong guildId, ulong userId), ModMailThread> _activeByUser = new();

    // Which guilds a user has mod mail enabled in (for DM routing when user is in multiple guilds)
    // If user is in exactly one guild with modmail, route there automatically
    private readonly ConcurrentDictionary<ulong, List<ulong>> _userGuildCache = new();

    public ModMailService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        // Load all enabled configs
        await using var uow = _db.GetDbContext();
        var configs = await uow.Set<ModMailConfig>()
            .AsNoTracking()
            .Where(c => c.Enabled)
            .ToListAsyncEF();

        foreach (var config in configs)
            _configs[config.GuildId] = config;

        // Load all open threads
        var openThreads = await uow.Set<ModMailThread>()
            .AsNoTracking()
            .Where(t => t.Status == ModMailThreadStatus.Open)
            .ToListAsyncEF();

        foreach (var thread in openThreads)
        {
            _activeByChannel[thread.ChannelId] = thread;
            _activeByUser[(thread.GuildId, thread.UserId)] = thread;
        }

        // Listen for DMs and guild messages
        _client.MessageReceived += OnMessageReceived;

        Log.Information("ModMail loaded {ConfigCount} configs, {ThreadCount} open threads",
            configs.Count, openThreads.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // MESSAGE HANDLER — Routes DMs to staff, staff replies to DMs
    // ═══════════════════════════════════════════════════════════

#pragma warning disable CS1998 // Async method lacks 'await' — fire-and-forget via Task.Run
    private async Task OnMessageReceived(SocketMessage msg)
    {
        if (msg.Author.IsBot || msg.Author.IsWebhook)
            return;

        if (msg is not SocketUserMessage userMsg)
            return;

        // DM from a user → route to staff thread
        if (msg.Channel is SocketDMChannel)
        {
            _ = Task.Run(async () =>
            {
                try { await HandleUserDmAsync(userMsg); }
                catch (Exception ex) { Log.Warning(ex, "ModMail DM handler failed"); }
            });
            return;
        }

        // Message in a guild channel → check if it's a modmail thread channel
        if (msg.Channel is SocketTextChannel guildChannel)
        {
            if (_activeByChannel.TryGetValue(guildChannel.Id, out _))
            {
                _ = Task.Run(async () =>
                {
                    try { await HandleStaffReplyAsync(userMsg, guildChannel); }
                    catch (Exception ex) { Log.Warning(ex, "ModMail staff reply handler failed"); }
                });
            }
        }
    }
#pragma warning restore CS1998

    private async Task HandleUserDmAsync(SocketUserMessage msg)
    {
        var userId = msg.Author.Id;

        // Find which guilds this user shares with the bot that have modmail enabled
        var modmailGuilds = _client.Guilds
            .Where(g => _configs.ContainsKey(g.Id) && g.GetUser(userId) is not null)
            .Select(g => g.Id)
            .ToList();

        if (modmailGuilds.Count == 0)
            return; // User isn't in any guild with modmail

        // Check for existing open thread in any of those guilds
        ModMailThread existingThread = null;
        ulong targetGuildId = 0;

        foreach (var gId in modmailGuilds)
        {
            if (_activeByUser.TryGetValue((gId, userId), out var thread))
            {
                existingThread = thread;
                targetGuildId = gId;
                break;
            }
        }

        if (existingThread is not null)
        {
            // Relay message to existing thread channel
            await RelayToThreadChannelAsync(existingThread, msg);
            return;
        }

        // No existing thread — create one
        // If user is in multiple modmail guilds, use the first one
        targetGuildId = modmailGuilds[0];

        // Check if user is blocked
        if (await IsBlockedAsync(targetGuildId, userId))
        {
            try
            {
                await msg.Author.SendMessageAsync(
                    "You are currently blocked from using mod mail in this server.");
            }
            catch { }
            return;
        }

        await CreateThreadFromDmAsync(targetGuildId, msg);
    }

    private async Task HandleStaffReplyAsync(SocketUserMessage msg, SocketTextChannel channel)
    {
        if (!_activeByChannel.TryGetValue(channel.Id, out var thread))
            return;

        // Don't relay bot commands (messages starting with common prefixes)
        var content = msg.Content;
        if (string.IsNullOrWhiteSpace(content) && msg.Attachments.Count == 0)
            return;
        if (content.StartsWith('.') || content.StartsWith('!') || content.StartsWith('/'))
            return;

        // Relay to user DMs
        await RelayToUserDmAsync(thread, msg);
    }

    // ═══════════════════════════════════════════════════════════
    // THREAD CREATION
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool success, ulong channelId, string error)> CreateThreadFromDmAsync(
        ulong guildId, SocketUserMessage dm)
    {
        if (!_configs.TryGetValue(guildId, out var config) || !config.Enabled)
            return (false, 0, "Mod mail is not enabled.");

        var guild = _client.GetGuild(guildId);
        if (guild is null)
            return (false, 0, "Guild not found.");

        var user = guild.GetUser(dm.Author.Id);
        var channelName = $"mm-{dm.Author.Username}".ToLowerInvariant();
        // Sanitize channel name — Discord allows alphanumeric and hyphens
        channelName = new string(channelName.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (channelName.Length > 90) channelName = channelName[..90];

        try
        {
            var category = config.CategoryId.HasValue
                ? guild.GetCategoryChannel(config.CategoryId.Value)
                : null;

            var channel = await guild.CreateTextChannelAsync(channelName, props =>
            {
                if (category is not null)
                    props.CategoryId = category.Id;
                props.Topic = $"Mod Mail from {dm.Author} ({dm.Author.Id})";
            });

            // Permissions: deny @everyone, allow staff role
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                new OverwritePermissions(viewChannel: PermValue.Deny));

            if (config.StaffRoleId.HasValue)
            {
                var staffRole = guild.GetRole(config.StaffRoleId.Value);
                if (staffRole is not null)
                    await channel.AddPermissionOverwriteAsync(staffRole,
                        new OverwritePermissions(
                            viewChannel: PermValue.Allow,
                            sendMessages: PermValue.Allow,
                            manageMessages: PermValue.Allow));
            }

            // Save thread to DB
            await using var uow = _db.GetDbContext();
            var thread = new ModMailThread
            {
                GuildId = guildId,
                UserId = dm.Author.Id,
                ChannelId = channel.Id,
                Status = ModMailThreadStatus.Open,
            };

            uow.Set<ModMailThread>().Add(thread);
            await uow.SaveChangesAsync();

            // Cache it
            _activeByChannel[channel.Id] = thread;
            _activeByUser[(guildId, dm.Author.Id)] = thread;

            // Send info embed in the thread channel
            var userInfo = user is not null
                ? $"{user.Mention} ({user.Username})"
                : $"<@{dm.Author.Id}> ({dm.Author.Username})";

            var roles = user?.Roles
                .Where(r => r.Id != guild.EveryoneRole.Id)
                .OrderByDescending(r => r.Position)
                .Select(r => r.Mention);

            var infoEmbed = _sender.CreateEmbed(guildId)
                .WithTitle("New Mod Mail Thread")
                .WithDescription($"User: {userInfo}\nAccount created: {dm.Author.CreatedAt:yyyy-MM-dd}\nJoined server: {user?.JoinedAt?.ToString("yyyy-MM-dd") ?? "Unknown"}")
                .WithThumbnailUrl(dm.Author.GetAvatarUrl() ?? dm.Author.GetDefaultAvatarUrl())
                .WithOkColor();

            if (roles is not null && roles.Any())
                infoEmbed.AddField("Roles", string.Join(", ", roles));

            infoEmbed.AddField("How to reply",
                "Just type in this channel — your messages will be relayed to the user's DMs.\nUse `.mmclose` or `.modmailclose` to close this thread.");

            var components = new ComponentBuilder()
                .WithButton("Close Thread", "modmail:close", ButtonStyle.Danger, new Emoji("\ud83d\udd12"))
                .Build();

            await channel.SendMessageAsync(embed: infoEmbed.Build(), components: components);

            // Relay the initial DM message
            await RelayToThreadChannelAsync(thread, dm);

            // DM the user confirmation
            try
            {
                var openMsg = (config.OpenMessage ?? "Your message has been sent to the staff team of **{server}**.")
                    .Replace("{server}", guild.Name)
                    .Replace("{user}", dm.Author.Mention);

                var dmEmbed = _sender.CreateEmbed(guildId)
                    .WithTitle($"Mod Mail — {guild.Name}")
                    .WithDescription(openMsg)
                    .WithOkColor();

                await dm.Author.SendMessageAsync(embed: dmEmbed.Build());
            }
            catch { } // Can't DM user — that's fine, message is already in the thread

            // Ping staff role
            if (config.StaffRoleId.HasValue)
            {
                try
                {
                    await channel.SendMessageAsync($"<@&{config.StaffRoleId.Value}> New mod mail from {dm.Author.Mention}");
                }
                catch { }
            }

            return (true, channel.Id, null);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to create mod mail thread");
            return (false, 0, "Failed to create mod mail channel. Check bot permissions.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MESSAGE RELAY
    // ═══════════════════════════════════════════════════════════

    private async Task RelayToThreadChannelAsync(ModMailThread thread, SocketUserMessage dm)
    {
        var guild = _client.GetGuild(thread.GuildId);
        var channel = guild?.GetTextChannel(thread.ChannelId);
        if (channel is null) return;

        var embed = _sender.CreateEmbed(thread.GuildId)
            .WithAuthor(dm.Author.ToString(), dm.Author.GetAvatarUrl())
            .WithDescription(dm.Content ?? "*(no text)*")
            .WithFooter("User message")
            .WithColor(new Color(0x3498DB)) // Blue for user messages
            .WithTimestamp(DateTimeOffset.UtcNow);

        // Handle attachments
        var attachmentUrls = new List<string>();
        if (dm.Attachments.Count > 0)
        {
            var attachText = string.Join("\n", dm.Attachments.Select(a => a.Url));
            embed.AddField("Attachments", attachText);
            attachmentUrls.AddRange(dm.Attachments.Select(a => a.Url));

            // Set first image attachment as the embed image
            var firstImage = dm.Attachments.FirstOrDefault(a =>
                a.ContentType?.StartsWith("image/") == true);
            if (firstImage is not null)
                embed.WithImageUrl(firstImage.Url);
        }

        await channel.SendMessageAsync(embed: embed.Build());

        // Archive the message
        await ArchiveMessageAsync(thread.Id, dm.Author.Id, false, dm.Content,
            attachmentUrls.Count > 0 ? string.Join("|", attachmentUrls) : null);
    }

    private async Task RelayToUserDmAsync(ModMailThread thread, SocketUserMessage staffMsg)
    {
        var user = _client.GetUser(thread.UserId);
        if (user is null) return;

        var guild = _client.GetGuild(thread.GuildId);
        var staffMember = guild?.GetUser(staffMsg.Author.Id);
        var staffName = staffMember?.DisplayName ?? staffMsg.Author.Username;

        try
        {
            var embed = _sender.CreateEmbed(thread.GuildId)
                .WithAuthor($"{staffName} — Staff", staffMsg.Author.GetAvatarUrl())
                .WithDescription(staffMsg.Content ?? "*(no text)*")
                .WithFooter($"Reply from {guild?.Name ?? "Server"}")
                .WithColor(new Color(0x2ECC71)) // Green for staff messages
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (staffMsg.Attachments.Count > 0)
            {
                var attachText = string.Join("\n", staffMsg.Attachments.Select(a => a.Url));
                embed.AddField("Attachments", attachText);

                var firstImage = staffMsg.Attachments.FirstOrDefault(a =>
                    a.ContentType?.StartsWith("image/") == true);
                if (firstImage is not null)
                    embed.WithImageUrl(firstImage.Url);
            }

            await user.SendMessageAsync(embed: embed.Build());

            // React with checkmark to confirm delivery
            await staffMsg.AddReactionAsync(new Emoji("\u2709\ufe0f"));
        }
        catch
        {
            // User has DMs closed — notify staff
            await staffMsg.AddReactionAsync(new Emoji("\u274c"));
            var channel = staffMsg.Channel as SocketTextChannel;
            if (channel is not null)
                await channel.SendMessageAsync("Could not DM this user. They may have DMs disabled.");
            return;
        }

        // Archive the message
        var attachmentList = staffMsg.Attachments.Count > 0
            ? string.Join("|", staffMsg.Attachments.Select(a => a.Url))
            : null;
        await ArchiveMessageAsync(thread.Id, staffMsg.Author.Id, true, staffMsg.Content, attachmentList);
    }

    private async Task ArchiveMessageAsync(int threadId, ulong authorId, bool isStaff, string content, string attachments)
    {
        await using var uow = _db.GetDbContext();

        uow.Set<ModMailMessage>().Add(new ModMailMessage
        {
            ThreadId = threadId,
            AuthorId = authorId,
            IsStaff = isStaff,
            Content = content,
            Attachments = attachments,
        });

        // Increment message count
        var thread = await uow.Set<ModMailThread>()
            .FirstOrDefaultAsyncEF(t => t.Id == threadId);
        if (thread is not null)
            thread.MessageCount++;

        await uow.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // THREAD CLOSE
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> CloseThreadByChannelAsync(ulong guildId, ulong channelId, ulong closedByUserId)
    {
        if (!_activeByChannel.TryGetValue(channelId, out var thread))
            return false;

        await using var uow = _db.GetDbContext();
        var dbThread = await uow.Set<ModMailThread>()
            .FirstOrDefaultAsyncEF(t => t.Id == thread.Id);

        if (dbThread is null)
            return false;

        dbThread.Status = ModMailThreadStatus.Closed;
        dbThread.ClosedAt = DateTime.UtcNow;
        dbThread.ClosedByUserId = closedByUserId;
        await uow.SaveChangesAsync();

        // Remove from caches
        _activeByChannel.TryRemove(channelId, out _);
        _activeByUser.TryRemove((guildId, thread.UserId), out _);

        // DM the user that the thread was closed
        if (_configs.TryGetValue(guildId, out var config))
        {
            try
            {
                var user = _client.GetUser(thread.UserId);
                if (user is not null)
                {
                    var guild = _client.GetGuild(guildId);
                    var closeMsg = (config.CloseMessage ?? "Your mod mail thread has been closed.")
                        .Replace("{server}", guild?.Name ?? "the server")
                        .Replace("{user}", user.Mention);

                    var embed = _sender.CreateEmbed(guildId)
                        .WithTitle("Mod Mail Closed")
                        .WithDescription(closeMsg)
                        .WithColor(new Color(0xFF0000));

                    await user.SendMessageAsync(embed: embed.Build());
                }
            }
            catch { }
        }

        // Post transcript to log channel
        await PostTranscriptAsync(guildId, dbThread);

        // Delete the channel after a short delay
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000);
                var guild = _client.GetGuild(guildId);
                var channel = guild?.GetTextChannel(channelId);
                if (channel is not null)
                    await channel.DeleteAsync();
            }
            catch { }
        });

        return true;
    }

    private async Task PostTranscriptAsync(ulong guildId, ModMailThread thread)
    {
        if (!_configs.TryGetValue(guildId, out var config) || !config.LogChannelId.HasValue)
            return;

        var guild = _client.GetGuild(guildId);
        var logChannel = guild?.GetTextChannel(config.LogChannelId.Value);
        if (logChannel is null) return;

        try
        {
            await using var uow = _db.GetDbContext();
            var messages = await uow.Set<ModMailMessage>()
                .AsNoTracking()
                .Where(m => m.ThreadId == thread.Id)
                .OrderBy(m => m.SentAt)
                .ToListAsyncEF();

            var user = _client.GetUser(thread.UserId);
            var duration = (thread.ClosedAt ?? DateTime.UtcNow) - thread.CreatedAt;

            var embed = _sender.CreateEmbed(guildId)
                .WithTitle($"Mod Mail Thread Closed")
                .WithDescription(
                    $"**User:** <@{thread.UserId}> ({user?.Username ?? "Unknown"})\n" +
                    $"**Messages:** {thread.MessageCount}\n" +
                    $"**Duration:** {duration.Humanize()}\n" +
                    $"**Closed by:** <@{thread.ClosedByUserId}>")
                .WithColor(new Color(0xFF0000))
                .WithTimestamp(DateTime.UtcNow);

            // Add last few messages as preview
            var lastMessages = messages.TakeLast(5).ToList();
            if (lastMessages.Count > 0)
            {
                var preview = string.Join("\n",
                    lastMessages.Select(m =>
                    {
                        var prefix = m.IsStaff ? "[Staff]" : "[User]";
                        var text = m.Content?.Length > 100 ? m.Content[..100] + "..." : m.Content ?? "(attachment)";
                        return $"{prefix} {text}";
                    }));
                embed.AddField($"Last {lastMessages.Count} Messages", preview);
            }

            await logChannel.SendMessageAsync(embed: embed.Build());

            // If there are many messages, also post a text file transcript
            if (messages.Count > 5)
            {
                var transcript = string.Join("\n",
                    messages.Select(m =>
                    {
                        var prefix = m.IsStaff ? "[Staff]" : "[User]";
                        var time = m.SentAt.ToString("yyyy-MM-dd HH:mm:ss");
                        var line = $"[{time}] {prefix} <@{m.AuthorId}>: {m.Content}";
                        if (!string.IsNullOrEmpty(m.Attachments))
                            line += $"\n  Attachments: {m.Attachments.Replace("|", ", ")}";
                        return line;
                    }));

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(transcript));
                await logChannel.SendFileAsync(stream, $"modmail-{thread.UserId}-{thread.Id}.txt",
                    "Full transcript attached.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to post mod mail transcript");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // BLOCK / UNBLOCK
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> IsBlockedAsync(ulong guildId, ulong userId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModMailBlock>()
            .AnyAsyncEF(b => b.GuildId == guildId && b.UserId == userId);
    }

    public async Task<bool> BlockUserAsync(ulong guildId, ulong userId, ulong blockedByUserId, string reason)
    {
        await using var uow = _db.GetDbContext();
        var existing = await uow.Set<ModMailBlock>()
            .FirstOrDefaultAsyncEF(b => b.GuildId == guildId && b.UserId == userId);

        if (existing is not null)
            return false; // Already blocked

        uow.Set<ModMailBlock>().Add(new ModMailBlock
        {
            GuildId = guildId,
            UserId = userId,
            BlockedByUserId = blockedByUserId,
            Reason = reason,
        });
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnblockUserAsync(ulong guildId, ulong userId)
    {
        await using var uow = _db.GetDbContext();
        var block = await uow.Set<ModMailBlock>()
            .FirstOrDefaultAsyncEF(b => b.GuildId == guildId && b.UserId == userId);

        if (block is null)
            return false;

        uow.Set<ModMailBlock>().Remove(block);
        await uow.SaveChangesAsync();
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // CONFIGURATION
    // ═══════════════════════════════════════════════════════════

    public async Task<ModMailConfig> GetOrCreateConfigAsync(ulong guildId)
    {
        if (_configs.TryGetValue(guildId, out var cached))
            return cached;

        await using var uow = _db.GetDbContext();
        var config = await uow.Set<ModMailConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new ModMailConfig { GuildId = guildId };
            uow.Set<ModMailConfig>().Add(config);
            await uow.SaveChangesAsync();
        }

        _configs[guildId] = config;
        return config;
    }

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var uow = _db.GetDbContext();
        var config = await uow.Set<ModMailConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new ModMailConfig { GuildId = guildId, Enabled = enabled };
            uow.Set<ModMailConfig>().Add(config);
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
        uow.Set<ModMailConfig>().Attach(config);
        config.CategoryId = categoryId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetLogChannelAsync(ulong guildId, ulong? channelId)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModMailConfig>().Attach(config);
        config.LogChannelId = channelId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetStaffRoleAsync(ulong guildId, ulong? roleId)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModMailConfig>().Attach(config);
        config.StaffRoleId = roleId;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetOpenMessageAsync(ulong guildId, string message)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModMailConfig>().Attach(config);
        config.OpenMessage = message;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task SetCloseMessageAsync(ulong guildId, string message)
    {
        var config = await GetOrCreateConfigAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModMailConfig>().Attach(config);
        config.CloseMessage = message;
        await uow.SaveChangesAsync();
        _configs[guildId] = config;
    }

    public async Task<List<ModMailThread>> GetRecentThreadsAsync(ulong guildId, int count = 10)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModMailThread>()
            .AsNoTracking()
            .Where(t => t.GuildId == guildId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsyncEF();
    }

    // ── Button Handler ──

    public void RegisterButtonHandler()
    {
        _client.InteractionCreated += interaction =>
        {
            if (interaction is not SocketMessageComponent component)
                return Task.CompletedTask;

            if (component.Data.CustomId != "modmail:close")
                return Task.CompletedTask;

            _ = Task.Run(async () =>
            {
                try
                {
                    await component.DeferAsync(ephemeral: true);
                    var guild = (component.Channel as SocketGuildChannel)?.Guild;
                    if (guild is null) return;

                    var success = await CloseThreadByChannelAsync(guild.Id, component.Channel.Id, component.User.Id);
                    if (success)
                        await component.FollowupAsync("Thread will be closed shortly.", ephemeral: true);
                    else
                        await component.FollowupAsync("This doesn't appear to be a mod mail thread.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ModMail close button handler failed");
                }
            });
            return Task.CompletedTask;
        };
    }
}
