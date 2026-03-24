#nullable disable
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class DehoistService : IReadyExecutor, INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    private readonly ConcurrentDictionary<ulong, DehoistConfig> _configs = new();

    // Characters commonly used to hoist to top of member list
    private static readonly Regex _hoistRegex = new(@"^[^a-zA-Z0-9\p{L}]+", RegexOptions.Compiled);

    public DehoistService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var uow = _db.GetDbContext();
        var configs = await uow.Set<DehoistConfig>()
            .AsNoTracking()
            .Where(c => c.Enabled)
            .ToListAsyncEF();

        foreach (var config in configs)
            _configs[config.GuildId] = config;

        _client.UserJoined += OnUserJoined;
        _client.GuildMemberUpdated += OnMemberUpdated;

        Log.Information("Dehoist loaded for {Count} guilds", configs.Count);
    }

    private Task OnUserJoined(SocketGuildUser user)
    {
        _ = Task.Run(() => DehoistUserAsync(user));
        return Task.CompletedTask;
    }

    private Task OnMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        // Only check if nickname changed
        if (before.HasValue && before.Value.Nickname == after.Nickname)
            return Task.CompletedTask;

        _ = Task.Run(() => DehoistUserAsync(after));
        return Task.CompletedTask;
    }

    private async Task DehoistUserAsync(SocketGuildUser user)
    {
        try
        {
            if (!_configs.TryGetValue(user.Guild.Id, out var config) || !config.Enabled)
                return;

            if (user.IsBot || user.GuildPermissions.Administrator)
                return;

            var displayName = user.Nickname ?? user.Username;
            if (string.IsNullOrEmpty(displayName))
                return;

            var match = _hoistRegex.Match(displayName);
            if (!match.Success)
                return;

            // Remove hoisting characters
            var cleaned = _hoistRegex.Replace(displayName, config.ReplacementPrefix ?? "");

            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "Dehoisted";

            if (cleaned == displayName)
                return;

            await user.ModifyAsync(u => u.Nickname = cleaned);
        }
        catch (Exception ex)
        {
            Log.Verbose(ex, "Dehoist failed for {User}", user);
        }
    }

    // ── Public API ──

    public async Task EnableAsync(ulong guildId, bool enabled)
    {
        await using var uow = _db.GetDbContext();
        var config = await uow.Set<DehoistConfig>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);

        if (config is null)
        {
            config = new DehoistConfig { GuildId = guildId, Enabled = enabled };
            uow.Set<DehoistConfig>().Add(config);
        }
        else
        {
            config.Enabled = enabled;
        }

        await uow.SaveChangesAsync();

        if (enabled)
            _configs[guildId] = config;
        else
            _configs.TryRemove(guildId, out _);
    }

    public async Task<DehoistConfig> GetConfigAsync(ulong guildId)
    {
        if (_configs.TryGetValue(guildId, out var config))
            return config;

        await using var uow = _db.GetDbContext();
        return await uow.Set<DehoistConfig>()
            .AsNoTracking()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId);
    }

    /// <summary>Dehoist all current members. Returns number of users dehoisted.</summary>
    public async Task<int> DehoistAllAsync(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null) return 0;

        var count = 0;
        foreach (var user in guild.Users)
        {
            if (user.IsBot || user.GuildPermissions.Administrator)
                continue;

            var displayName = user.Nickname ?? user.Username;
            if (string.IsNullOrEmpty(displayName) || !_hoistRegex.IsMatch(displayName))
                continue;

            try
            {
                var cleaned = _hoistRegex.Replace(displayName, "");
                if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "Dehoisted";
                await user.ModifyAsync(u => u.Nickname = cleaned);
                count++;
                await Task.Delay(500); // Rate limit friendly
            }
            catch { }
        }

        return count;
    }
}
