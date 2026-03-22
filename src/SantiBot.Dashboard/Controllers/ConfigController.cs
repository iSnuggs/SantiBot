using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public ConfigController(DbService db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

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

        return Ok(new { success = true });
    }

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

    [HttpGet("xp")]
    public async Task<IActionResult> GetXp(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var settings = await ctx.Set<XpSettings>()
            .ToLinqToDBTable()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId);

        return Ok(new
        {
            configured = settings is not null,
            guildId = guildId.ToString(),
        });
    }

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

public class StarboardUpdateModel
{
    public ulong? ChannelId { get; set; }
    public int? Threshold { get; set; }
    public string? Emoji { get; set; }
    public bool? AllowSelfStar { get; set; }
}
