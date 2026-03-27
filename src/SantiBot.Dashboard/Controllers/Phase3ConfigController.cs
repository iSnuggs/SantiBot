using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Dashboard.Services;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// API endpoints for all Phase 3 features (the Dyno features we added).
/// Each section handles one dashboard page's data.
/// </summary>
[ApiController]
[Route("api/guilds/{guildId}/config")]
[Authorize]
public class Phase3ConfigController : ControllerBase
{
    private readonly DbService _db;

    public Phase3ConfigController(DbService db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────────
    // SETTINGS — general guild configuration
    // ──────────────────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gc = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            configured = gc is not null,
            prefix = gc?.Prefix ?? ".",
            timeZoneId = gc?.TimeZoneId,
            locale = gc?.Locale,
            deleteMessageOnCommand = gc?.DeleteMessageOnCommand ?? false,
            autoAssignRoleIds = gc?.AutoAssignRoleIds,
            disableGlobalExpressions = gc?.DisableGlobalExpressions ?? false,
        });
    }

    [HttpPatch("settings")]
    public async Task<IActionResult> UpdateSettings(ulong guildId, [FromBody] SettingsUpdateModel model)
    {
        await using var ctx = _db.GetDbContext();
        var gc = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        if (gc is null)
            return NotFound(new { error = "Guild not configured yet. Run a command in the server first." });

        if (model.Prefix is not null) gc.Prefix = model.Prefix;
        if (model.TimeZoneId is not null) gc.TimeZoneId = model.TimeZoneId;
        if (model.Locale is not null) gc.Locale = model.Locale;
        if (model.DeleteMessageOnCommand.HasValue) gc.DeleteMessageOnCommand = model.DeleteMessageOnCommand.Value;
        if (model.DisableGlobalExpressions.HasValue) gc.DisableGlobalExpressions = model.DisableGlobalExpressions.Value;
        await ctx.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // ──────────────────────────────────────────────
    // AUTOMOD — advanced auto-moderation rules
    // ──────────────────────────────────────────────

    [HttpGet("automod")]
    public async Task<IActionResult> GetAutomod(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var rules = await ctx.GetTable<AutomodRule>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(rules.Select(r => new
        {
            id = r.Id,
            enabled = r.Enabled,
            filterType = r.FilterType.ToString(),
            action = r.Action.ToString(),
            actionDurationMinutes = r.ActionDurationMinutes,
            threshold = r.Threshold,
            timeWindowSeconds = r.TimeWindowSeconds,
            patternOrList = r.PatternOrList,
            customResponseText = r.CustomResponseText,
        }));
    }

    // ──────────────────────────────────────────────
    // AUTO-RESPONDER — keyword-triggered responses
    // ──────────────────────────────────────────────

    [HttpGet("autoresponder")]
    public async Task<IActionResult> GetAutoResponder(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var responses = await ctx.GetTable<AutoResponse>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(responses.Select(r => new
        {
            id = r.Id,
            enabled = r.Enabled,
            trigger = r.Trigger,
            triggerType = r.TriggerType.ToString(),
            responseText = r.ResponseText,
            responseType = r.ResponseType.ToString(),
            deleteTrigger = r.DeleteTrigger,
            userCooldownSeconds = r.UserCooldownSeconds,
            channelCooldownSeconds = r.ChannelCooldownSeconds,
        }));
    }

    // ──────────────────────────────────────────────
    // TICKETS — support ticket system config + list
    // ──────────────────────────────────────────────

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<TicketConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        var tickets = await ctx.GetTable<Ticket>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsyncLinqToDB();

        return Ok(new
        {
            config = config is null ? null : new
            {
                enabled = config.Enabled,
                categoryId = config.CategoryId?.ToString(),
                logChannelId = config.LogChannelId?.ToString(),
                supportRoleId = config.SupportRoleId?.ToString(),
                maxTicketsPerUser = config.MaxTicketsPerUser,
                welcomeMessage = config.WelcomeMessage,
            },
            tickets = tickets.Select(t => new
            {
                id = t.Id,
                ticketNumber = t.TicketNumber,
                creatorUserId = t.CreatorUserId.ToString(),
                claimedByUserId = t.ClaimedByUserId?.ToString(),
                channelId = t.ChannelId.ToString(),
                status = t.Status.ToString(),
                topic = t.Topic,
                createdAt = t.CreatedAt,
                closedAt = t.ClosedAt,
            }),
        });
    }

    // ──────────────────────────────────────────────
    // AUTOBAN — automatic ban rules
    // ──────────────────────────────────────────────

    [HttpGet("autoban")]
    public async Task<IActionResult> GetAutoban(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var rules = await ctx.GetTable<AutobanRule>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(rules.Select(r => new
        {
            id = r.Id,
            enabled = r.Enabled,
            ruleType = r.RuleType.ToString(),
            minAccountAgeHours = r.MinAccountAgeHours,
            usernamePatterns = r.UsernamePatterns,
            action = r.Action.ToString(),
            reason = r.Reason,
        }));
    }

    // ──────────────────────────────────────────────
    // AUTO DELETE — per-channel auto-deletion rules
    // ──────────────────────────────────────────────

    [HttpGet("autodelete")]
    public async Task<IActionResult> GetAutoDelete(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var rules = await ctx.GetTable<AutoDeleteRule>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(rules.Select(r => new
        {
            id = r.Id,
            channelId = r.ChannelId.ToString(),
            enabled = r.Enabled,
            delaySeconds = r.DelaySeconds,
            useFilter = r.UseFilter,
            filter = r.Filter,
            ignorePinned = r.IgnorePinned,
        }));
    }

    // ──────────────────────────────────────────────
    // AUTO MESSAGE — scheduled/recurring messages
    // ──────────────────────────────────────────────

    [HttpGet("automessage")]
    public async Task<IActionResult> GetAutoMessage(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var messages = await ctx.GetTable<ScheduledMessage>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(messages.Select(m => new
        {
            id = m.Id,
            channelId = m.ChannelId.ToString(),
            content = m.Content,
            isRecurring = m.IsRecurring,
            scheduledAt = m.ScheduledAt,
            interval = m.Interval?.ToString(),
            lastSentAt = m.LastSentAt,
            isActive = m.IsActive,
            creatorUserId = m.CreatorUserId.ToString(),
        }));
    }

    // ──────────────────────────────────────────────
    // ANTI-RAID — verification gate + lockdown
    // ──────────────────────────────────────────────

    [HttpGet("antiraid")]
    public async Task<IActionResult> GetAntiRaid(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var gate = await ctx.GetTable<VerificationGate>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            configured = gate is not null,
            enabled = gate?.Enabled ?? false,
            verifyChannelId = gate?.VerifyChannelId?.ToString(),
            verifiedRoleId = gate?.VerifiedRoleId?.ToString(),
            verifyMessage = gate?.VerifyMessage,
            autoLockdownEnabled = gate?.AutoLockdownEnabled ?? false,
            lockdownJoinThreshold = gate?.LockdownJoinThreshold ?? 10,
            lockdownTimeWindowSeconds = gate?.LockdownTimeWindowSeconds ?? 30,
            isLockedDown = gate?.IsLockedDown ?? false,
        });
    }

    // ──────────────────────────────────────────────
    // VOICE TEXT LINKING — voice↔text channel pairs
    // ──────────────────────────────────────────────

    [HttpGet("voicetext")]
    public async Task<IActionResult> GetVoiceTextLinks(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var links = await ctx.GetTable<VoiceTextLink>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        var dehoist = await ctx.GetTable<DehoistConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            links = links.Select(l => new
            {
                id = l.Id,
                voiceChannelId = l.VoiceChannelId.ToString(),
                textChannelId = l.TextChannelId.ToString(),
            }),
            dehoist = new
            {
                enabled = dehoist?.Enabled ?? false,
                replacementPrefix = dehoist?.ReplacementPrefix ?? "[dehoist]",
            },
        });
    }

    // ──────────────────────────────────────────────
    // WELCOME IMAGES — custom welcome banners
    // ──────────────────────────────────────────────

    [HttpGet("welcome")]
    public async Task<IActionResult> GetWelcome(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var config = await ctx.GetTable<WelcomeImageConfig>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            configured = config is not null,
            enabled = config?.Enabled ?? false,
            channelId = config?.ChannelId?.ToString(),
            backgroundUrl = config?.BackgroundUrl,
            accentColor = config?.AccentColor ?? "#0c95e9",
            welcomeText = config?.WelcomeText ?? "Welcome to the server!",
            subtitleText = config?.SubtitleText,
        });
    }

    // ──────────────────────────────────────────────
    // MOD CASES — moderation case log + settings
    // ──────────────────────────────────────────────

    [HttpGet("modcases")]
    public async Task<IActionResult> GetModCases(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.GetTable<ModSettings>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        var cases = await ctx.GetTable<ModCase>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.CaseNumber)
            .Take(50)
            .ToListAsyncLinqToDB();

        var autopunish = await ctx.GetTable<AutoPunishConfig>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(new
        {
            settings = new
            {
                modLogChannelId = settings?.ModLogChannelId?.ToString(),
                dmOnAction = settings?.DmOnAction ?? false,
                deleteModCommands = settings?.DeleteModCommands ?? false,
                protectedRoleIds = settings?.ProtectedRoleIds,
            },
            autopunish = autopunish.Select(a => new
            {
                id = a.Id,
                caseCount = a.CaseCount,
                timeWindowHours = a.TimeWindowHours,
                action = a.Action.ToString(),
                actionDurationMinutes = a.ActionDurationMinutes,
            }),
            cases = cases.Select(c => new
            {
                caseNumber = c.CaseNumber,
                caseType = c.CaseType.ToString(),
                targetUserId = c.TargetUserId.ToString(),
                moderatorUserId = c.ModeratorUserId.ToString(),
                reason = c.Reason,
                createdAt = c.CreatedAt,
            }),
        });
    }

    // ──────────────────────────────────────────────
    // SUGGESTIONS — community suggestion system
    // ──────────────────────────────────────────────

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var suggestions = await ctx.GetTable<SuggestionModel>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .ToListAsyncLinqToDB();

        return Ok(suggestions.Select(s => new
        {
            id = s.Id,
            authorId = s.AuthorId.ToString(),
            content = s.Content,
            status = s.Status.ToString(),
            statusReason = s.StatusReason,
            channelId = s.ChannelId.ToString(),
            messageId = s.MessageId.ToString(),
        }));
    }

    // ──────────────────────────────────────────────
    // REACTION ROLES — emoji→role mapping
    // ──────────────────────────────────────────────

    [HttpGet("reactionroles")]
    public async Task<IActionResult> GetReactionRoles(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var roles = await ctx.Set<ReactionRoleV2>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(roles.Select(r => new
        {
            id = r.Id,
            channelId = r.ChannelId.ToString(),
            messageId = r.MessageId.ToString(),
            emote = r.Emote,
            roleId = r.RoleId.ToString(),
            group = r.Group,
            levelReq = r.LevelReq,
        }));
    }

    // ──────────────────────────────────────────────
    // STREAM NOTIFICATIONS — followed streams + feeds
    // ──────────────────────────────────────────────

    [HttpGet("streams")]
    public async Task<IActionResult> GetStreams(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var streams = await ctx.Set<FollowedStream>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        var feeds = await ctx.Set<FeedSub>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(new
        {
            streams = streams.Select(s => new
            {
                id = s.Id,
                username = s.Username,
                prettyName = s.PrettyName,
                type = s.Type.ToString(),
                channelId = s.ChannelId.ToString(),
                message = s.Message,
            }),
            feeds = feeds.Select(f => new
            {
                id = f.Id,
                url = f.Url,
                channelId = f.ChannelId.ToString(),
                message = f.Message,
            }),
        });
    }

    // ──────────────────────────────────────────────
    // REMINDERS — server reminders
    // ──────────────────────────────────────────────

    [HttpGet("reminders")]
    public async Task<IActionResult> GetReminders(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var reminders = await ctx.Set<Reminder>()
            .ToLinqToDBTable()
            .Where(x => x.ServerId == guildId)
            .OrderBy(x => x.When)
            .Take(50)
            .ToListAsyncLinqToDB();

        return Ok(reminders.Select(r => new
        {
            id = r.Id,
            message = r.Message,
            when = r.When,
            channelId = r.ChannelId.ToString(),
            userId = r.UserId.ToString(),
            isPrivate = r.IsPrivate,
        }));
    }
}

// ──────────────────────────────────────────────
// Request body models
// ──────────────────────────────────────────────

public class SettingsUpdateModel
{
    public string? Prefix { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Locale { get; set; }
    public bool? DeleteMessageOnCommand { get; set; }
    public bool? DisableGlobalExpressions { get; set; }
}
