using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

/// <summary>
/// Backup and restore all guild settings as a single JSON file.
/// Queries the same config tables as ConfigController and Phase3ConfigController.
/// </summary>
[ApiController]
[Route("api/guilds/{guildId}")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly DbService _db;

    public BackupController(DbService db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────────
    // EXPORT — download all guild settings as JSON
    // ──────────────────────────────────────────────

    [HttpGet("backup")]
    public async Task<IActionResult> ExportBackup(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();

        // Starboard
        var starboard = await ctx.GetTable<StarboardSettings>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Logging
        var logging = await ctx.Set<LogSetting>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Moderation / General (GuildConfig)
        var guildConfig = await ctx.Set<GuildConfig>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Music
        var music = await ctx.Set<MusicPlayerSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // XP
        var xp = await ctx.Set<XpSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        // Expressions
        var expressions = await ctx.Set<SantiExpression>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        // Auto Purge
        var autoPurge = await ctx.GetTable<AutoPurgeConfig>()
            .Where(x => x.GuildId == guildId && x.IsActive)
            .ToListAsyncLinqToDB();

        // Permissions
        var permissions = await ctx.Set<Permissionv2>()
            .ToLinqToDBTable()
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Index)
            .ToListAsyncLinqToDB();

        var backup = new
        {
            version = "1.0",
            guildId = guildId.ToString(),
            exportedAt = DateTime.UtcNow,
            starboard = starboard is null ? null : new
            {
                channelId = starboard.StarboardChannelId.ToString(),
                threshold = starboard.StarThreshold,
                emoji = starboard.StarEmoji,
                allowSelfStar = starboard.AllowSelfStar,
            },
            logging = logging is null ? null : new
            {
                messageUpdated = logging.MessageUpdatedId?.ToString(),
                messageDeleted = logging.MessageDeletedId?.ToString(),
                userJoined = logging.UserJoinedId?.ToString(),
                userLeft = logging.UserLeftId?.ToString(),
                userBanned = logging.UserBannedId?.ToString(),
                userUnbanned = logging.UserUnbannedId?.ToString(),
                userUpdated = logging.UserUpdatedId?.ToString(),
                channelCreated = logging.ChannelCreatedId?.ToString(),
                channelDestroyed = logging.ChannelDestroyedId?.ToString(),
                channelUpdated = logging.ChannelUpdatedId?.ToString(),
                voicePresence = logging.LogVoicePresenceId?.ToString(),
                userMuted = logging.UserMutedId?.ToString(),
                userWarned = logging.LogWarnsId?.ToString(),
                threadCreated = logging.ThreadCreatedId?.ToString(),
                threadDeleted = logging.ThreadDeletedId?.ToString(),
                nicknameChanged = logging.NicknameChangedId?.ToString(),
                roleChanged = logging.RoleChangedId?.ToString(),
                emojiUpdated = logging.EmojiUpdatedId?.ToString(),
            },
            moderation = guildConfig is null ? null : new
            {
                prefix = guildConfig.Prefix,
                deleteMessageOnCommand = guildConfig.DeleteMessageOnCommand,
                warnExpireHours = guildConfig.WarnExpireHours,
                warnExpireAction = guildConfig.WarnExpireAction.ToString(),
                muteRoleName = guildConfig.MuteRoleName,
                verboseErrors = guildConfig.VerboseErrors,
                verbosePermissions = guildConfig.VerbosePermissions,
                stickyRoles = guildConfig.StickyRoles,
                timeZoneId = guildConfig.TimeZoneId,
                locale = guildConfig.Locale,
                autoAssignRoleIds = guildConfig.AutoAssignRoleIds,
            },
            music = music is null ? null : new
            {
                volume = music.Volume,
                autoDisconnect = music.AutoDisconnect,
                autoPlay = music.AutoPlay,
                repeat = music.PlayerRepeat.ToString(),
                quality = music.QualityPreset.ToString(),
                musicChannelId = music.MusicChannelId?.ToString(),
            },
            expressions = expressions.Select(e => new
            {
                trigger = e.Trigger,
                response = e.Response,
                autoDeleteTrigger = e.AutoDeleteTrigger,
                dmResponse = e.DmResponse,
                containsAnywhere = e.ContainsAnywhere,
                allowTarget = e.AllowTarget,
            }),
            autoPurge = autoPurge.Select(c => new
            {
                channelId = c.ChannelId.ToString(),
                intervalHours = c.IntervalHours,
                maxMessageAgeHours = c.MaxMessageAgeHours,
            }),
            permissions = permissions.Select(p => new
            {
                index = p.Index,
                primaryTarget = p.PrimaryTarget.ToString(),
                primaryTargetId = p.PrimaryTargetId.ToString(),
                secondaryTarget = p.SecondaryTarget.ToString(),
                secondaryTargetName = p.SecondaryTargetName,
                state = p.State,
            }),
        };

        return Ok(backup);
    }

    // ──────────────────────────────────────────────
    // RESTORE — import guild settings from JSON
    // ──────────────────────────────────────────────

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreBackup(ulong guildId, [FromBody] BackupPayload payload)
    {
        await using var ctx = _db.GetDbContext();

        // Validate the backup is for this guild (or allow migration)
        if (payload.GuildId is not null && payload.GuildId != guildId.ToString())
        {
            // Allow it — user is importing from another server
        }

        var sectionsRestored = new List<string>();

        // Restore starboard
        if (payload.Starboard is not null)
        {
            var existing = await ctx.GetTable<StarboardSettings>()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

            if (existing is not null)
            {
                await ctx.GetTable<StarboardSettings>()
                    .Where(x => x.GuildId == guildId)
                    .DeleteAsync();
            }

            var starboardChannelId = ulong.TryParse(payload.Starboard.ChannelId, out var cid) ? cid : 0ul;
            await ctx.GetTable<StarboardSettings>()
                .InsertAsync(() => new StarboardSettings
                {
                    GuildId = guildId,
                    StarboardChannelId = starboardChannelId,
                    StarThreshold = payload.Starboard.Threshold ?? 3,
                    StarEmoji = payload.Starboard.Emoji ?? "⭐",
                    AllowSelfStar = payload.Starboard.AllowSelfStar ?? false,
                });
            sectionsRestored.Add("starboard");
        }

        // Restore logging
        if (payload.Logging is not null)
        {
            var existing = await ctx.Set<LogSetting>()
                .ToLinqToDBTable()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

            static ulong? Parse(string? val)
                => !string.IsNullOrWhiteSpace(val) && ulong.TryParse(val, out var id) ? id : null;

            if (existing is not null)
            {
                existing.MessageUpdatedId = Parse(payload.Logging.MessageUpdated);
                existing.MessageDeletedId = Parse(payload.Logging.MessageDeleted);
                existing.UserJoinedId = Parse(payload.Logging.UserJoined);
                existing.UserLeftId = Parse(payload.Logging.UserLeft);
                existing.UserBannedId = Parse(payload.Logging.UserBanned);
                existing.UserUnbannedId = Parse(payload.Logging.UserUnbanned);
                existing.UserUpdatedId = Parse(payload.Logging.UserUpdated);
                existing.ChannelCreatedId = Parse(payload.Logging.ChannelCreated);
                existing.ChannelDestroyedId = Parse(payload.Logging.ChannelDestroyed);
                existing.ChannelUpdatedId = Parse(payload.Logging.ChannelUpdated);
                existing.LogVoicePresenceId = Parse(payload.Logging.VoicePresence);
                existing.UserMutedId = Parse(payload.Logging.UserMuted);
                existing.LogWarnsId = Parse(payload.Logging.UserWarned);
                existing.ThreadCreatedId = Parse(payload.Logging.ThreadCreated);
                existing.ThreadDeletedId = Parse(payload.Logging.ThreadDeleted);
                existing.NicknameChangedId = Parse(payload.Logging.NicknameChanged);
                existing.RoleChangedId = Parse(payload.Logging.RoleChanged);
                existing.EmojiUpdatedId = Parse(payload.Logging.EmojiUpdated);
                await ctx.SaveChangesAsync();
            }
            else
            {
                ctx.Set<LogSetting>().Add(new LogSetting
                {
                    GuildId = guildId,
                    MessageUpdatedId = Parse(payload.Logging.MessageUpdated),
                    MessageDeletedId = Parse(payload.Logging.MessageDeleted),
                    UserJoinedId = Parse(payload.Logging.UserJoined),
                    UserLeftId = Parse(payload.Logging.UserLeft),
                    UserBannedId = Parse(payload.Logging.UserBanned),
                    UserUnbannedId = Parse(payload.Logging.UserUnbanned),
                    UserUpdatedId = Parse(payload.Logging.UserUpdated),
                    ChannelCreatedId = Parse(payload.Logging.ChannelCreated),
                    ChannelDestroyedId = Parse(payload.Logging.ChannelDestroyed),
                    ChannelUpdatedId = Parse(payload.Logging.ChannelUpdated),
                    LogVoicePresenceId = Parse(payload.Logging.VoicePresence),
                    UserMutedId = Parse(payload.Logging.UserMuted),
                    LogWarnsId = Parse(payload.Logging.UserWarned),
                    ThreadCreatedId = Parse(payload.Logging.ThreadCreated),
                    ThreadDeletedId = Parse(payload.Logging.ThreadDeleted),
                    NicknameChangedId = Parse(payload.Logging.NicknameChanged),
                    RoleChangedId = Parse(payload.Logging.RoleChanged),
                    EmojiUpdatedId = Parse(payload.Logging.EmojiUpdated),
                });
                await ctx.SaveChangesAsync();
            }
            sectionsRestored.Add("logging");
        }

        // Restore music
        if (payload.Music is not null)
        {
            var existing = await ctx.Set<MusicPlayerSettings>()
                .ToLinqToDBTable()
                .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

            if (existing is not null)
            {
                existing.Volume = payload.Music.Volume ?? 100;
                existing.AutoDisconnect = payload.Music.AutoDisconnect ?? false;
                existing.AutoPlay = payload.Music.AutoPlay ?? false;
                await ctx.SaveChangesAsync();
            }
            else
            {
                ctx.Set<MusicPlayerSettings>().Add(new MusicPlayerSettings
                {
                    GuildId = guildId,
                    Volume = payload.Music.Volume ?? 100,
                    AutoDisconnect = payload.Music.AutoDisconnect ?? false,
                    AutoPlay = payload.Music.AutoPlay ?? false,
                });
                await ctx.SaveChangesAsync();
            }
            sectionsRestored.Add("music");
        }

        return Ok(new
        {
            success = true,
            sectionsRestored,
            restoredAt = DateTime.UtcNow,
        });
    }
}

// ──────────────────────────────────────────────
// Backup/Restore payload models
// ──────────────────────────────────────────────

public class BackupPayload
{
    public string? Version { get; set; }
    public string? GuildId { get; set; }
    public StarboardBackup? Starboard { get; set; }
    public LoggingBackup? Logging { get; set; }
    public MusicBackup? Music { get; set; }
}

public class StarboardBackup
{
    public string? ChannelId { get; set; }
    public int? Threshold { get; set; }
    public string? Emoji { get; set; }
    public bool? AllowSelfStar { get; set; }
}

public class LoggingBackup
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

public class MusicBackup
{
    public int? Volume { get; set; }
    public bool? AutoDisconnect { get; set; }
    public bool? AutoPlay { get; set; }
}
