using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SantiBot.Dashboard.Hubs;
using SantiBot.Dashboard.Services;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

[ApiController]
[Route("api/guilds/{guildId}/config")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly DbService _db;
    private readonly JwtService _jwt;
    private readonly IHubContext<DashboardHub> _hub;

    public ConfigController(DbService db, JwtService jwt, IHubContext<DashboardHub> hub)
    {
        _db = db;
        _jwt = jwt;
        _hub = hub;
    }

    /// <summary>
    /// Broadcast a config change to all dashboard users viewing this guild.
    /// The "section" tells the frontend which page's data changed (e.g., "starboard", "logging").
    /// </summary>
    private async Task BroadcastChange(ulong guildId, string section)
    {
        await _hub.Clients.Group($"guild:{guildId}")
            .SendAsync("ConfigUpdated", section);
    }

    // ──────────────────────────────────────────────
    // OVERVIEW — aggregate stats from the database
    // ──────────────────────────────────────────────

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Count active features for this guild
        var hasStarboard = await ctx.GetTable<StarboardSettings>()
            .AnyAsyncLinqToDB(x => x.GuildId == guildId);
        var purgeCount = await ctx.GetTable<AutoPurgeConfig>()
            .CountAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);
        var giveawayCount = await ctx.GetTable<GiveawayModel>()
            .CountAsyncLinqToDB(x => x.GuildId == guildId);
        var pollCount = await ctx.GetTable<PollModel>()
            .CountAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);
        var formCount = await ctx.GetTable<FormModel>()
            .CountAsyncLinqToDB(x => x.GuildId == guildId && x.IsActive);
        var expressionCount = await ctx.Set<SantiExpression>()
            .ToLinqToDBTable()
            .CountAsyncLinqToDB(x => x.GuildId == guildId);

        // Count how many features are enabled
        var activeFeatures = 0;
        if (hasStarboard) activeFeatures++;
        if (purgeCount > 0) activeFeatures++;
        if (giveawayCount > 0) activeFeatures++;
        if (pollCount > 0) activeFeatures++;
        if (formCount > 0) activeFeatures++;
        if (expressionCount > 0) activeFeatures++;

        return Ok(new
        {
            activeFeatures,
            giveaways = giveawayCount,
            activePolls = pollCount,
            activeForms = formCount,
            expressions = expressionCount,
            autoPurgeChannels = purgeCount,
            starboardEnabled = hasStarboard,
        });
    }

    // ──────────────────────────────────────────────
    // STARBOARD — read and update starboard settings
    // ──────────────────────────────────────────────

    [HttpGet("starboard")]
    public async Task<IActionResult> GetStarboard(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.GetTable<StarboardSettings>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (settings is null)
            return Ok(new { enabled = false });

        return Ok(new
        {
            enabled = true,
            channelId = settings.StarboardChannelId.ToString(),
            threshold = settings.StarThreshold,
            emoji = settings.StarEmoji,
            allowSelfStar = settings.AllowSelfStar,
        });
    }

    [HttpPatch("starboard")]
    public async Task<IActionResult> UpdateStarboard(ulong guildId, [FromBody] StarboardUpdateModel model)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.GetTable<StarboardSettings>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            await ctx.GetTable<StarboardSettings>()
                .InsertAsync(() => new StarboardSettings
                {
                    GuildId = guildId,
                    StarboardChannelId = model.ChannelId ?? 0,
                    StarThreshold = model.Threshold ?? 3,
                    StarEmoji = model.Emoji ?? "⭐",
                    AllowSelfStar = model.AllowSelfStar ?? false,
                });
        }
        else
        {
            var query = ctx.GetTable<StarboardSettings>().Where(x => x.GuildId == guildId);

            if (model.ChannelId.HasValue)
                query = (IQueryable<StarboardSettings>)query.Set(x => x.StarboardChannelId, model.ChannelId.Value);
            if (model.Threshold.HasValue)
                query = (IQueryable<StarboardSettings>)query.Set(x => x.StarThreshold, model.Threshold.Value);
            if (model.Emoji is not null)
                query = (IQueryable<StarboardSettings>)query.Set(x => x.StarEmoji, model.Emoji);
            if (model.AllowSelfStar.HasValue)
                query = (IQueryable<StarboardSettings>)query.Set(x => x.AllowSelfStar, model.AllowSelfStar.Value);
        }

        await BroadcastChange(guildId, "starboard");
        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // LOGGING — read and update per-event log channels
    // ──────────────────────────────────────────────

    [HttpGet("logging")]
    public async Task<IActionResult> GetLogging(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.Set<LogSetting>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (settings is null)
            return Ok(new { enabled = false });

        return Ok(new
        {
            enabled = true,
            messageUpdated = settings.MessageUpdatedId?.ToString(),
            messageDeleted = settings.MessageDeletedId?.ToString(),
            userJoined = settings.UserJoinedId?.ToString(),
            userLeft = settings.UserLeftId?.ToString(),
            userBanned = settings.UserBannedId?.ToString(),
            userUnbanned = settings.UserUnbannedId?.ToString(),
            userUpdated = settings.UserUpdatedId?.ToString(),
            channelCreated = settings.ChannelCreatedId?.ToString(),
            channelDestroyed = settings.ChannelDestroyedId?.ToString(),
            channelUpdated = settings.ChannelUpdatedId?.ToString(),
            voicePresence = settings.LogVoicePresenceId?.ToString(),
            userMuted = settings.UserMutedId?.ToString(),
            userWarned = settings.LogWarnsId?.ToString(),
            threadCreated = settings.ThreadCreatedId?.ToString(),
            threadDeleted = settings.ThreadDeletedId?.ToString(),
            nicknameChanged = settings.NicknameChangedId?.ToString(),
            roleChanged = settings.RoleChangedId?.ToString(),
            emojiUpdated = settings.EmojiUpdatedId?.ToString(),
        });
    }

    [HttpPatch("logging")]
    public async Task<IActionResult> UpdateLogging(ulong guildId, [FromBody] LoggingUpdateModel model)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.Set<LogSetting>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Helper to parse channel ID strings — returns null if empty
        static ulong? ParseChannel(string? val)
            => !string.IsNullOrWhiteSpace(val) && ulong.TryParse(val, out var id) ? id : null;

        if (existing is null)
        {
            // Create a new log settings row
            ctx.Set<LogSetting>().Add(new LogSetting
            {
                GuildId = guildId,
                MessageUpdatedId = ParseChannel(model.MessageUpdated),
                MessageDeletedId = ParseChannel(model.MessageDeleted),
                UserJoinedId = ParseChannel(model.UserJoined),
                UserLeftId = ParseChannel(model.UserLeft),
                UserBannedId = ParseChannel(model.UserBanned),
                UserUnbannedId = ParseChannel(model.UserUnbanned),
                UserUpdatedId = ParseChannel(model.UserUpdated),
                ChannelCreatedId = ParseChannel(model.ChannelCreated),
                ChannelDestroyedId = ParseChannel(model.ChannelDestroyed),
                ChannelUpdatedId = ParseChannel(model.ChannelUpdated),
                LogVoicePresenceId = ParseChannel(model.VoicePresence),
                UserMutedId = ParseChannel(model.UserMuted),
                LogWarnsId = ParseChannel(model.UserWarned),
                ThreadCreatedId = ParseChannel(model.ThreadCreated),
                ThreadDeletedId = ParseChannel(model.ThreadDeleted),
                NicknameChangedId = ParseChannel(model.NicknameChanged),
                RoleChangedId = ParseChannel(model.RoleChanged),
                EmojiUpdatedId = ParseChannel(model.EmojiUpdated),
            });
            await ctx.SaveChangesAsync();
        }
        else
        {
            // Update only the fields that were sent
            if (model.MessageUpdated is not null) existing.MessageUpdatedId = ParseChannel(model.MessageUpdated);
            if (model.MessageDeleted is not null) existing.MessageDeletedId = ParseChannel(model.MessageDeleted);
            if (model.UserJoined is not null) existing.UserJoinedId = ParseChannel(model.UserJoined);
            if (model.UserLeft is not null) existing.UserLeftId = ParseChannel(model.UserLeft);
            if (model.UserBanned is not null) existing.UserBannedId = ParseChannel(model.UserBanned);
            if (model.UserUnbanned is not null) existing.UserUnbannedId = ParseChannel(model.UserUnbanned);
            if (model.UserUpdated is not null) existing.UserUpdatedId = ParseChannel(model.UserUpdated);
            if (model.ChannelCreated is not null) existing.ChannelCreatedId = ParseChannel(model.ChannelCreated);
            if (model.ChannelDestroyed is not null) existing.ChannelDestroyedId = ParseChannel(model.ChannelDestroyed);
            if (model.ChannelUpdated is not null) existing.ChannelUpdatedId = ParseChannel(model.ChannelUpdated);
            if (model.VoicePresence is not null) existing.LogVoicePresenceId = ParseChannel(model.VoicePresence);
            if (model.UserMuted is not null) existing.UserMutedId = ParseChannel(model.UserMuted);
            if (model.UserWarned is not null) existing.LogWarnsId = ParseChannel(model.UserWarned);
            if (model.ThreadCreated is not null) existing.ThreadCreatedId = ParseChannel(model.ThreadCreated);
            if (model.ThreadDeleted is not null) existing.ThreadDeletedId = ParseChannel(model.ThreadDeleted);
            if (model.NicknameChanged is not null) existing.NicknameChangedId = ParseChannel(model.NicknameChanged);
            if (model.RoleChanged is not null) existing.RoleChangedId = ParseChannel(model.RoleChanged);
            if (model.EmojiUpdated is not null) existing.EmojiUpdatedId = ParseChannel(model.EmojiUpdated);
            await ctx.SaveChangesAsync();
        }

        await BroadcastChange(guildId, "logging");
        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // XP — read and update XP/leveling settings
    // ──────────────────────────────────────────────

    [HttpGet("xp")]
    public async Task<IActionResult> GetXp(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.Set<XpSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Count how many users have XP in this guild
        var userCount = await ctx.Set<UserXpStats>()
            .ToLinqToDBTable()
            .CountAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            configured = settings is not null,
            guildId = guildId.ToString(),
            trackedUsers = userCount,
        });
    }

    // ──────────────────────────────────────────────
    // MUSIC — read and update music player settings
    // ──────────────────────────────────────────────

    [HttpGet("music")]
    public async Task<IActionResult> GetMusic(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.Set<MusicPlayerSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (settings is null)
            return Ok(new
            {
                configured = false,
                volume = 100,
                autoDisconnect = false,
                autoPlay = false,
                repeat = "Queue",
                quality = "Highest",
            });

        return Ok(new
        {
            configured = true,
            volume = settings.Volume,
            autoDisconnect = settings.AutoDisconnect,
            autoPlay = settings.AutoPlay,
            repeat = settings.PlayerRepeat.ToString(),
            quality = settings.QualityPreset.ToString(),
            musicChannelId = settings.MusicChannelId?.ToString(),
        });
    }

    [HttpPatch("music")]
    public async Task<IActionResult> UpdateMusic(ulong guildId, [FromBody] MusicUpdateModel model)
    {
        await using var ctx = _db.GetDbContext();

        var existing = await ctx.Set<MusicPlayerSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (existing is null)
        {
            // Create new music settings for this guild
            ctx.Set<MusicPlayerSettings>().Add(new MusicPlayerSettings
            {
                GuildId = guildId,
                Volume = model.Volume ?? 100,
                AutoDisconnect = model.AutoDisconnect ?? false,
                AutoPlay = model.AutoPlay ?? false,
            });
            await ctx.SaveChangesAsync();
        }
        else
        {
            if (model.Volume.HasValue) existing.Volume = model.Volume.Value;
            if (model.AutoDisconnect.HasValue) existing.AutoDisconnect = model.AutoDisconnect.Value;
            if (model.AutoPlay.HasValue) existing.AutoPlay = model.AutoPlay.Value;
            await ctx.SaveChangesAsync();
        }

        await BroadcastChange(guildId, "music");
        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // MODERATION — read and update moderation settings
    // ──────────────────────────────────────────────

    [HttpGet("moderation")]
    public async Task<IActionResult> GetModeration(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gc = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (gc is null)
            return Ok(new
            {
                configured = false,
                deleteMessageOnCommand = false,
                warnExpireHours = 0,
                muteRoleName = "SantiMute",
                verboseErrors = true,
            });

        return Ok(new
        {
            configured = true,
            prefix = gc.Prefix,
            deleteMessageOnCommand = gc.DeleteMessageOnCommand,
            warnExpireHours = gc.WarnExpireHours,
            warnExpireAction = gc.WarnExpireAction.ToString(),
            muteRoleName = gc.MuteRoleName ?? "SantiMute",
            verboseErrors = gc.VerboseErrors,
            verbosePermissions = gc.VerbosePermissions,
            stickyRoles = gc.StickyRoles,
            timeZoneId = gc.TimeZoneId,
        });
    }

    [HttpPatch("moderation")]
    public async Task<IActionResult> UpdateModeration(ulong guildId, [FromBody] ModerationUpdateModel model)
    {
        await using var ctx = _db.GetDbContext();
        var gc = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (gc is null)
            return NotFound(new { error = "Guild not configured yet. Run a command in the server first." });

        if (model.DeleteMessageOnCommand.HasValue) gc.DeleteMessageOnCommand = model.DeleteMessageOnCommand.Value;
        if (model.WarnExpireHours.HasValue) gc.WarnExpireHours = model.WarnExpireHours.Value;
        if (model.MuteRoleName is not null) gc.MuteRoleName = model.MuteRoleName;
        if (model.VerboseErrors.HasValue) gc.VerboseErrors = model.VerboseErrors.Value;
        if (model.StickyRoles.HasValue) gc.StickyRoles = model.StickyRoles.Value;
        await ctx.SaveChangesAsync();

        await BroadcastChange(guildId, "moderation");
        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // PERMISSIONS — read command permission overrides
    // ──────────────────────────────────────────────

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gc = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        var perms = await ctx.Set<Permissionv2>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Index)
            .ToListAsyncLinqToDB();

        return Ok(new
        {
            verbosePermissions = gc?.VerbosePermissions ?? true,
            permissionRole = gc?.PermissionRole,
            overrides = perms.Select(p => new
            {
                index = p.Index,
                primaryTarget = p.PrimaryTarget.ToString(),
                primaryTargetId = p.PrimaryTargetId.ToString(),
                secondaryTarget = p.SecondaryTarget.ToString(),
                secondaryTargetName = p.SecondaryTargetName,
                state = p.State,
            }),
        });
    }

    // ──────────────────────────────────────────────
    // EXPRESSIONS — read custom trigger/response pairs
    // ──────────────────────────────────────────────

    [HttpGet("expressions")]
    public async Task<IActionResult> GetExpressions(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var exprs = await ctx.Set<SantiExpression>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(exprs.Select(e => new
        {
            id = e.Id,
            trigger = e.Trigger,
            response = e.Response,
            autoDeleteTrigger = e.AutoDeleteTrigger,
            dmResponse = e.DmResponse,
            containsAnywhere = e.ContainsAnywhere,
            allowTarget = e.AllowTarget,
        }));
    }

    // ──────────────────────────────────────────────
    // AUTO PURGE — read active purge configurations
    // ──────────────────────────────────────────────

    [HttpGet("autopurge")]
    public async Task<IActionResult> GetAutoPurge(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var configs = await ctx.GetTable<AutoPurgeConfig>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();

        return Ok(configs.Select(c => new
        {
            id = c.Id,
            channelId = c.ChannelId.ToString(),
            intervalHours = c.IntervalHours,
            maxMessageAgeHours = c.MaxMessageAgeHours,
        }));
    }

    // ──────────────────────────────────────────────
    // GIVEAWAYS — read active and past giveaways
    // ──────────────────────────────────────────────

    [HttpGet("giveaways")]
    public async Task<IActionResult> GetGiveaways(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var giveaways = await ctx.GetTable<GiveawayModel>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(giveaways.Select(g => new
        {
            id = g.Id,
            message = g.Message,
            channelId = g.ChannelId.ToString(),
            endsAt = g.EndsAt,
            winnerCount = g.WinnerCount,
            requiredRoleId = g.RequiredRoleId?.ToString(),
        }));
    }

    // ──────────────────────────────────────────────
    // POLLS — read active polls
    // ──────────────────────────────────────────────

    [HttpGet("polls")]
    public async Task<IActionResult> GetPolls(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var polls = await ctx.GetTable<PollModel>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();

        return Ok(polls.Select(p => new
        {
            id = p.Id,
            question = p.Question,
            channelId = p.ChannelId.ToString(),
            endsAt = p.EndsAt,
        }));
    }

    // ──────────────────────────────────────────────
    // FORMS — read active forms
    // ──────────────────────────────────────────────

    [HttpGet("forms")]
    public async Task<IActionResult> GetForms(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var forms = await ctx.GetTable<FormModel>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();

        return Ok(forms.Select(f => new
        {
            id = f.Id,
            title = f.Title,
            responseChannelId = f.ResponseChannelId.ToString(),
            questionsJson = f.QuestionsJson,
        }));
    }
}

// ──────────────────────────────────────────────
// Request body models for PATCH endpoints
// ──────────────────────────────────────────────

public class StarboardUpdateModel
{
    public ulong? ChannelId { get; set; }
    public int? Threshold { get; set; }
    public string? Emoji { get; set; }
    public bool? AllowSelfStar { get; set; }
}

public class LoggingUpdateModel
{
    public string? MessageUpdated { get; set; }
    public string? MessageDeleted { get; set; }
    public string? UserJoined { get; set; }
    public string? UserLeft { get; set; }
    public string? UserBanned { get; set; }
    public string? UserUnbanned { get; set; }
    public string? UserUpdated { get; set; }
    public string? ChannelCreated { get; set; }
    public string? ChannelDestroyed { get; set; }
    public string? ChannelUpdated { get; set; }
    public string? VoicePresence { get; set; }
    public string? UserMuted { get; set; }
    public string? UserWarned { get; set; }
    public string? ThreadCreated { get; set; }
    public string? ThreadDeleted { get; set; }
    public string? NicknameChanged { get; set; }
    public string? RoleChanged { get; set; }
    public string? EmojiUpdated { get; set; }
}

public class MusicUpdateModel
{
    public int? Volume { get; set; }
    public bool? AutoDisconnect { get; set; }
    public bool? AutoPlay { get; set; }
}

public class ModerationUpdateModel
{
    public bool? DeleteMessageOnCommand { get; set; }
    public int? WarnExpireHours { get; set; }
    public string? MuteRoleName { get; set; }
    public bool? VerboseErrors { get; set; }
    public bool? StickyRoles { get; set; }
}
