#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class VerificationGateService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    private readonly ConcurrentDictionary<ulong, VerificationGate> _gates = new();

    // Track recent joins for auto-lockdown: GuildId → list of join timestamps
    private readonly ConcurrentDictionary<ulong, List<DateTime>> _joinTracker = new();

    public VerificationGateService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var gates = await uow.Set<VerificationGate>()
            .AsNoTracking()
            .Where(g => g.Enabled)
            .ToListAsyncEF();

        foreach (var gate in gates)
            _gates[gate.GuildId] = gate;

        _client.InteractionCreated += OnInteractionCreated;
        _client.UserJoined += OnUserJoined;

        Log.Information("Verification gates loaded for {Count} guilds", gates.Count);
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!_gates.TryGetValue(user.Guild.Id, out var gate))
                    return;

                // Track joins for auto-lockdown
                if (gate.AutoLockdownEnabled)
                    await CheckAutoLockdownAsync(gate, user.Guild);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Verification gate join handler failed");
            }
        });

        return Task.CompletedTask;
    }

    private async Task CheckAutoLockdownAsync(VerificationGate gate, SocketGuild guild)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-gate.LockdownTimeWindowSeconds);

        var joins = _joinTracker.GetOrAdd(guild.Id, _ => new());
        lock (joins)
        {
            joins.RemoveAll(t => t < cutoff);
            joins.Add(now);

            if (joins.Count < gate.LockdownJoinThreshold)
                return;
        }

        if (gate.IsLockedDown)
            return;

        // TRIGGER LOCKDOWN
        Log.Warning("Auto-lockdown triggered in {Guild}: {Count} joins in {Window}s",
            guild.Name, gate.LockdownJoinThreshold, gate.LockdownTimeWindowSeconds);

        try
        {
            // Set verification level to highest
            await guild.ModifyAsync(g => g.VerificationLevel = VerificationLevel.Extreme);

            gate.IsLockedDown = true;

            // Persist lockdown state
            await using var uow = _db.GetDbContext();
            var dbGate = await uow.Set<VerificationGate>()
                .FirstOrDefaultAsyncEF(g => g.GuildId == guild.Id);
            if (dbGate is not null)
            {
                dbGate.IsLockedDown = true;
                await uow.SaveChangesAsync();
            }

            // Notify in the verification channel or first available text channel
            var notifyChannel = gate.VerifyChannelId.HasValue
                ? guild.GetTextChannel(gate.VerifyChannelId.Value)
                : guild.DefaultChannel;

            if (notifyChannel is not null)
            {
                var embed = _sender.CreateEmbed(guild.Id)
                    .WithTitle("🚨 Auto-Lockdown Activated")
                    .WithDescription(
                        $"**{gate.LockdownJoinThreshold} users joined in {gate.LockdownTimeWindowSeconds} seconds.**\n" +
                        "Server verification level has been set to Extreme.\n" +
                        "Use `.lockdown off` to disable lockdown when the raid is over.")
                    .WithColor(new Color(0xFF0000))
                    .WithTimestamp(DateTime.UtcNow);

                await notifyChannel.SendMessageAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to activate auto-lockdown");
        }
    }

    private async Task OnInteractionCreated(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component)
            return;

        if (component.Data.CustomId != "verify:confirm")
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await component.DeferAsync(ephemeral: true);

                var guild = (component.Channel as SocketGuildChannel)?.Guild;
                if (guild is null) return;

                if (!_gates.TryGetValue(guild.Id, out var gate) || !gate.Enabled)
                {
                    await component.FollowupAsync("Verification is not enabled.", ephemeral: true);
                    return;
                }

                if (!gate.VerifiedRoleId.HasValue)
                {
                    await component.FollowupAsync("Verified role is not configured.", ephemeral: true);
                    return;
                }

                var user = guild.GetUser(component.User.Id);
                if (user is null) return;

                var role = guild.GetRole(gate.VerifiedRoleId.Value);
                if (role is null)
                {
                    await component.FollowupAsync("Verified role not found.", ephemeral: true);
                    return;
                }

                if (user.Roles.Any(r => r.Id == role.Id))
                {
                    await component.FollowupAsync("You're already verified!", ephemeral: true);
                    return;
                }

                await user.AddRoleAsync(role);
                await component.FollowupAsync("✅ You're verified! Welcome to the server!", ephemeral: true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Verification button handler failed");
            }
        });
    }

    // ── Public API ──

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var uow = _db.GetDbContext();
        var gate = await uow.Set<VerificationGate>()
            .FirstOrDefaultAsyncEF(g => g.GuildId == guildId);

        if (gate is null)
        {
            gate = new VerificationGate { GuildId = guildId, Enabled = enabled };
            uow.Set<VerificationGate>().Add(gate);
        }
        else
        {
            gate.Enabled = enabled;
        }

        await uow.SaveChangesAsync();

        if (enabled)
            _gates[guildId] = gate;
        else
            _gates.TryRemove(guildId, out _);
    }

    public async Task SetVerifiedRoleAsync(ulong guildId, ulong? roleId)
    {
        var gate = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<VerificationGate>().Attach(gate);
        gate.VerifiedRoleId = roleId;
        await uow.SaveChangesAsync();
        _gates[guildId] = gate;
    }

    public async Task SetVerifyChannelAsync(ulong guildId, ulong? channelId)
    {
        var gate = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<VerificationGate>().Attach(gate);
        gate.VerifyChannelId = channelId;
        await uow.SaveChangesAsync();
        _gates[guildId] = gate;
    }

    public async Task SetVerifyMessageAsync(ulong guildId, string message)
    {
        var gate = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<VerificationGate>().Attach(gate);
        gate.VerifyMessage = message;
        await uow.SaveChangesAsync();
        _gates[guildId] = gate;
    }

    public async Task SetAutoLockdownAsync(ulong guildId, bool enabled, int joinThreshold = 10, int timeWindowSeconds = 10)
    {
        var gate = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<VerificationGate>().Attach(gate);
        gate.AutoLockdownEnabled = enabled;
        gate.LockdownJoinThreshold = joinThreshold;
        gate.LockdownTimeWindowSeconds = timeWindowSeconds;
        await uow.SaveChangesAsync();
        _gates[guildId] = gate;
    }

    public async Task SetLockdownAsync(ulong guildId, bool locked)
    {
        var gate = await GetOrCreateAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<VerificationGate>().Attach(gate);
        gate.IsLockedDown = locked;
        await uow.SaveChangesAsync();
        _gates[guildId] = gate;
    }

    public async Task<bool> SendVerifyPanelAsync(ulong guildId, ITextChannel channel)
    {
        if (!_gates.TryGetValue(guildId, out var gate))
            return false;

        var embed = _sender.CreateEmbed(guildId)
            .WithTitle("✅ Verification Required")
            .WithDescription(gate.VerifyMessage ?? "Click the button below to verify and get access!")
            .WithOkColor();

        var components = new ComponentBuilder()
            .WithButton("Verify", "verify:confirm", ButtonStyle.Success, new Emoji("✅"))
            .Build();

        var msg = await channel.SendMessageAsync(embed: embed.Build(), components: components);

        gate.VerifyMessageId = msg.Id;
        gate.VerifyChannelId = channel.Id;

        await using var uow = _db.GetDbContext();
        var dbGate = await uow.Set<VerificationGate>().FirstOrDefaultAsyncEF(g => g.GuildId == guildId);
        if (dbGate is not null)
        {
            dbGate.VerifyMessageId = msg.Id;
            dbGate.VerifyChannelId = channel.Id;
            await uow.SaveChangesAsync();
        }

        return true;
    }

    public async Task<VerificationGate> GetConfigAsync(ulong guildId)
    {
        if (_gates.TryGetValue(guildId, out var gate))
            return gate;

        await using var uow = _db.GetDbContext();
        return await uow.Set<VerificationGate>()
            .AsNoTracking()
            .FirstOrDefaultAsyncEF(g => g.GuildId == guildId);
    }

    /// <summary>Mass-bans all users who joined during the raid window.</summary>
    public async Task<int> MassJoinBanAsync(ulong guildId, int seconds)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return 0;

        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-seconds);
        var recentJoins = guild.Users
            .Where(u => u.JoinedAt.HasValue && u.JoinedAt.Value > cutoff && !u.GuildPermissions.Administrator)
            .ToList();

        var banned = 0;
        foreach (var user in recentJoins)
        {
            try
            {
                await guild.AddBanAsync(user, reason: $"Mass join ban: joined during raid window ({seconds}s)");
                banned++;
            }
            catch { }
        }

        return banned;
    }

    private async Task<VerificationGate> GetOrCreateAsync(ulong guildId)
    {
        if (_gates.TryGetValue(guildId, out var cached))
            return cached;

        await using var uow = _db.GetDbContext();
        var gate = await uow.Set<VerificationGate>()
            .FirstOrDefaultAsyncEF(g => g.GuildId == guildId);

        if (gate is null)
        {
            gate = new VerificationGate { GuildId = guildId };
            uow.Set<VerificationGate>().Add(gate);
            await uow.SaveChangesAsync();
        }

        _gates[guildId] = gate;
        return gate;
    }
}
