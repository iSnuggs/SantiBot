#nullable disable
using Microsoft.EntityFrameworkCore;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Administration;

public sealed class ModCaseService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly IMessageSenderService _sender;

    // Cache mod settings per guild
    private readonly ConcurrentDictionary<ulong, ModSettings> _settingsCache = new();

    public ModCaseService(DbService db, DiscordSocketClient client, IMessageSenderService sender)
    {
        _db = db;
        _client = client;
        _sender = sender;
    }

    // ── Case Management ──

    /// <summary>Creates a new mod case and returns it with the assigned case number.</summary>
    public async Task<ModCase> CreateCaseAsync(ulong guildId, ModCaseType caseType,
        ulong targetUserId, ulong moderatorUserId, string reason, int durationMinutes = 0)
    {
        await using var uow = _db.GetDbContext();

        // Get next case number for this guild
        var maxCase = await uow.Set<ModCase>()
            .Where(c => c.GuildId == guildId)
            .OrderByDescending(c => c.CaseNumber)
            .Select(c => c.CaseNumber)
            .FirstOrDefaultAsyncEF();

        var modCase = new ModCase
        {
            GuildId = guildId,
            CaseNumber = maxCase + 1,
            CaseType = caseType,
            TargetUserId = targetUserId,
            ModeratorUserId = moderatorUserId,
            Reason = reason ?? "No reason provided",
            DurationMinutes = durationMinutes,
            CreatedAt = DateTime.UtcNow,
        };

        uow.Set<ModCase>().Add(modCase);
        await uow.SaveChangesAsync();

        // Post to mod log channel
        _ = Task.Run(async () => await PostToModLogAsync(guildId, modCase));

        // DM the user if configured
        _ = Task.Run(async () => await DmUserOnActionAsync(guildId, modCase));

        // Check auto-punish escalation
        _ = Task.Run(async () => await CheckAutoPunishAsync(guildId, targetUserId));

        return modCase;
    }

    public async Task<ModCase> GetCaseAsync(ulong guildId, int caseNumber)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModCase>()
            .AsNoTracking()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId && c.CaseNumber == caseNumber);
    }

    public async Task<List<ModCase>> GetUserCasesAsync(ulong guildId, ulong userId, int limit = 25)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModCase>()
            .AsNoTracking()
            .Where(c => c.GuildId == guildId && c.TargetUserId == userId)
            .OrderByDescending(c => c.CaseNumber)
            .Take(limit)
            .ToListAsyncEF();
    }

    public async Task<List<ModCase>> GetRecentCasesAsync(ulong guildId, int limit = 25)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModCase>()
            .AsNoTracking()
            .Where(c => c.GuildId == guildId)
            .OrderByDescending(c => c.CaseNumber)
            .Take(limit)
            .ToListAsyncEF();
    }

    public async Task<bool> UpdateReasonAsync(ulong guildId, int caseNumber, string reason)
    {
        await using var uow = _db.GetDbContext();
        var modCase = await uow.Set<ModCase>()
            .FirstOrDefaultAsyncEF(c => c.GuildId == guildId && c.CaseNumber == caseNumber);

        if (modCase is null)
            return false;

        modCase.Reason = reason;
        await uow.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetCaseCountAsync(ulong guildId, ulong userId, TimeSpan? window = null)
    {
        await using var uow = _db.GetDbContext();
        var query = uow.Set<ModCase>()
            .Where(c => c.GuildId == guildId && c.TargetUserId == userId && c.CaseType != ModCaseType.Note);

        if (window.HasValue)
        {
            var cutoff = DateTime.UtcNow - window.Value;
            query = query.Where(c => c.CreatedAt >= cutoff);
        }

        return await query.CountAsyncEF();
    }

    // ── Mod Notes ──

    public async Task<ModNote> AddNoteAsync(ulong guildId, ulong targetUserId, ulong moderatorUserId, string content)
    {
        await using var uow = _db.GetDbContext();
        var note = new ModNote
        {
            GuildId = guildId,
            TargetUserId = targetUserId,
            ModeratorUserId = moderatorUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };

        uow.Set<ModNote>().Add(note);
        await uow.SaveChangesAsync();
        return note;
    }

    public async Task<List<ModNote>> GetNotesAsync(ulong guildId, ulong targetUserId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<ModNote>()
            .AsNoTracking()
            .Where(n => n.GuildId == guildId && n.TargetUserId == targetUserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsyncEF();
    }

    public async Task<bool> DeleteNoteAsync(ulong guildId, int noteId)
    {
        await using var uow = _db.GetDbContext();
        var note = await uow.Set<ModNote>()
            .FirstOrDefaultAsyncEF(n => n.Id == noteId && n.GuildId == guildId);

        if (note is null)
            return false;

        uow.Set<ModNote>().Remove(note);
        await uow.SaveChangesAsync();
        return true;
    }

    // ── Mod Settings ──

    public async Task<ModSettings> GetSettingsAsync(ulong guildId)
    {
        if (_settingsCache.TryGetValue(guildId, out var cached))
            return cached;

        await using var uow = _db.GetDbContext();
        var settings = await uow.Set<ModSettings>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId);

        if (settings is null)
        {
            settings = new ModSettings { GuildId = guildId };
            uow.Set<ModSettings>().Add(settings);
            await uow.SaveChangesAsync();
        }

        _settingsCache[guildId] = settings;
        return settings;
    }

    public async Task SetModLogChannelAsync(ulong guildId, ulong? channelId)
    {
        var settings = await GetOrCreateSettingsAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModSettings>().Attach(settings);
        settings.ModLogChannelId = channelId;
        await uow.SaveChangesAsync();
        _settingsCache[guildId] = settings;
    }

    public async Task SetDmOnActionAsync(ulong guildId, bool enabled)
    {
        var settings = await GetOrCreateSettingsAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModSettings>().Attach(settings);
        settings.DmOnAction = enabled;
        await uow.SaveChangesAsync();
        _settingsCache[guildId] = settings;
    }

    public async Task SetDmTemplateAsync(ulong guildId, string template)
    {
        var settings = await GetOrCreateSettingsAsync(guildId);
        await using var uow = _db.GetDbContext();
        uow.Set<ModSettings>().Attach(settings);
        settings.DmTemplate = template;
        await uow.SaveChangesAsync();
        _settingsCache[guildId] = settings;
    }

    // ── Auto-Punish Escalation ──

    public async Task<AutoPunishConfig> AddAutoPunishAsync(ulong guildId, int caseCount,
        PunishmentAction action, int timeWindowHours = 0, int actionDurationMinutes = 0)
    {
        await using var uow = _db.GetDbContext();
        var config = new AutoPunishConfig
        {
            GuildId = guildId,
            CaseCount = caseCount,
            Action = action,
            TimeWindowHours = timeWindowHours,
            ActionDurationMinutes = actionDurationMinutes,
        };

        uow.Set<AutoPunishConfig>().Add(config);
        await uow.SaveChangesAsync();
        return config;
    }

    public async Task<List<AutoPunishConfig>> GetAutoPunishConfigsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.Set<AutoPunishConfig>()
            .AsNoTracking()
            .Where(c => c.GuildId == guildId)
            .OrderBy(c => c.CaseCount)
            .ToListAsyncEF();
    }

    public async Task<bool> RemoveAutoPunishAsync(ulong guildId, int configId)
    {
        await using var uow = _db.GetDbContext();
        var config = await uow.Set<AutoPunishConfig>()
            .FirstOrDefaultAsyncEF(c => c.Id == configId && c.GuildId == guildId);

        if (config is null)
            return false;

        uow.Set<AutoPunishConfig>().Remove(config);
        await uow.SaveChangesAsync();
        return true;
    }

    // ── Protected Roles ──

    public async Task<bool> IsProtectedAsync(ulong guildId, IGuildUser user)
    {
        var settings = await GetSettingsAsync(guildId);
        if (string.IsNullOrEmpty(settings.ProtectedRoleIds))
            return false;

        var protectedRoles = settings.ProtectedRoleIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ulong.Parse)
            .ToHashSet();

        return user.RoleIds.Any(r => protectedRoles.Contains(r));
    }

    // ── Internal Helpers ──

    private async Task<ModSettings> GetOrCreateSettingsAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        var settings = await uow.Set<ModSettings>()
            .FirstOrDefaultAsyncEF(s => s.GuildId == guildId);

        if (settings is null)
        {
            settings = new ModSettings { GuildId = guildId };
            uow.Set<ModSettings>().Add(settings);
            await uow.SaveChangesAsync();
        }

        return settings;
    }

    private async Task PostToModLogAsync(ulong guildId, ModCase modCase)
    {
        try
        {
            var settings = await GetSettingsAsync(guildId);
            if (settings.ModLogChannelId is null)
                return;

            var guild = _client.GetGuild(guildId);
            if (guild is null)
                return;

            var channel = guild.GetTextChannel(settings.ModLogChannelId.Value);
            if (channel is null)
                return;

            var target = guild.GetUser(modCase.TargetUserId);
            var mod = guild.GetUser(modCase.ModeratorUserId);

            var embed = _sender.CreateEmbed(guildId)
                .WithTitle($"Case #{modCase.CaseNumber} — {modCase.CaseType}")
                .AddField("User", target?.ToString() ?? $"<@{modCase.TargetUserId}>", true)
                .AddField("Moderator", mod?.ToString() ?? $"<@{modCase.ModeratorUserId}>", true)
                .AddField("Reason", modCase.Reason)
                .WithTimestamp(modCase.CreatedAt)
                .WithColor(GetCaseColor(modCase.CaseType));

            if (modCase.DurationMinutes > 0)
                embed.AddField("Duration", $"{modCase.DurationMinutes} minutes", true);

            var msg = await channel.SendMessageAsync(embed: embed.Build());

            // Save log message ID for future edits
            await using var uow = _db.GetDbContext();
            var dbCase = await uow.Set<ModCase>()
                .FirstOrDefaultAsyncEF(c => c.Id == modCase.Id);
            if (dbCase is not null)
            {
                dbCase.LogMessageId = msg.Id;
                dbCase.LogChannelId = channel.Id;
                await uow.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to post mod case to log channel");
        }
    }

    private async Task DmUserOnActionAsync(ulong guildId, ModCase modCase)
    {
        try
        {
            // Don't DM for notes or unmutes/unbans
            if (modCase.CaseType is ModCaseType.Note or ModCaseType.Unmute or ModCaseType.Unban)
                return;

            var settings = await GetSettingsAsync(guildId);
            if (!settings.DmOnAction)
                return;

            var guild = _client.GetGuild(guildId);
            var user = guild?.GetUser(modCase.TargetUserId);
            if (user is null)
                return;

            var template = settings.DmTemplate ?? "You have been **{action}** in **{server}**.\nReason: {reason}";
            var message = template
                .Replace("{action}", modCase.CaseType.ToString().ToLowerInvariant())
                .Replace("{reason}", modCase.Reason)
                .Replace("{server}", guild.Name)
                .Replace("{duration}", modCase.DurationMinutes > 0 ? $"{modCase.DurationMinutes} minutes" : "permanent");

            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(message);
        }
        catch
        {
            // User may have DMs disabled — silently ignore
        }
    }

    private async Task CheckAutoPunishAsync(ulong guildId, ulong userId)
    {
        try
        {
            await using var uow = _db.GetDbContext();
            var configs = await uow.Set<AutoPunishConfig>()
                .AsNoTracking()
                .Where(c => c.GuildId == guildId)
                .OrderByDescending(c => c.CaseCount)
                .ToListAsyncEF();

            if (configs.Count == 0)
                return;

            foreach (var config in configs)
            {
                TimeSpan? window = config.TimeWindowHours > 0
                    ? TimeSpan.FromHours(config.TimeWindowHours)
                    : null;

                var count = await GetCaseCountAsync(guildId, userId, window);

                if (count >= config.CaseCount)
                {
                    var guild = _client.GetGuild(guildId);
                    var user = guild?.GetUser(userId);
                    if (user is null || user.GuildPermissions.Administrator)
                        return;

                    Log.Information("AutoPunish triggered for {User} in {Guild}: {Count} cases → {Action}",
                        userId, guildId, count, config.Action);

                    switch (config.Action)
                    {
                        case PunishmentAction.Kick:
                            await user.KickAsync("Auto-punish: too many mod cases");
                            break;
                        case PunishmentAction.Ban:
                            await guild.AddBanAsync(user, reason: "Auto-punish: too many mod cases");
                            break;
                        case PunishmentAction.TimeOut:
                            var duration = config.ActionDurationMinutes > 0
                                ? TimeSpan.FromMinutes(config.ActionDurationMinutes)
                                : TimeSpan.FromHours(1);
                            await user.SetTimeOutAsync(duration);
                            break;
                        case PunishmentAction.Mute:
                            var muteRole = guild.Roles.FirstOrDefault(r => r.Name.Equals("Muted", StringComparison.OrdinalIgnoreCase));
                            if (muteRole is not null)
                                await user.AddRoleAsync(muteRole);
                            break;
                    }

                    break; // Only apply the highest threshold
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "AutoPunish check failed");
        }
    }

    private static Color GetCaseColor(ModCaseType type) => type switch
    {
        ModCaseType.Warn => new Color(0xFFA500),     // Orange
        ModCaseType.Mute or ModCaseType.TempMute => new Color(0xFFFF00), // Yellow
        ModCaseType.Kick => new Color(0xFF6347),     // Red-Orange
        ModCaseType.Ban or ModCaseType.TempBan => new Color(0xFF0000),   // Red
        ModCaseType.Unban or ModCaseType.Unmute => new Color(0x00FF00),  // Green
        ModCaseType.Note => new Color(0x808080),     // Gray
        _ => new Color(0x0C95E9),                    // SantiBot blue
    };
}
