using System.Text.Json;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SantiBot.Dashboard.Services;
using SantiBot.Db.Models;
using SantiBot.Services;

namespace SantiBot.Dashboard.Controllers;

[ApiController]
[Route("api/guilds/{guildId}/embeds")]
[Authorize]
public class EmbedController : ControllerBase
{
    private readonly DbService _db;
    private readonly JwtService _jwt;

    public EmbedController(DbService db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("preview")]
    public IActionResult Preview(ulong guildId, [FromBody] EmbedModel model)
    {
        // Validate and return the embed as it would appear
        return Ok(new
        {
            success = true,
            preview = new
            {
                model.Title,
                model.Description,
                model.Color,
                model.Author,
                model.Footer,
                model.ImageUrl,
                model.ThumbnailUrl,
                model.Fields,
            }
        });
    }

    [HttpGet("saved")]
    public async Task<IActionResult> GetSaved(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        var embeds = await ctx.GetTable<SavedEmbed>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();

        return Ok(embeds.Select(e => new
        {
            e.Id,
            e.Name,
            e.EmbedJson,
            creatorId = e.CreatorId.ToString(),
        }));
    }

    [HttpPost("saved")]
    public async Task<IActionResult> SaveEmbed(ulong guildId, [FromBody] SaveEmbedRequest request)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        if (userId is null)
            return Unauthorized();

        var json = JsonSerializer.Serialize(request.Embed);

        await using var ctx = _db.GetDbContext();
        var saved = await ctx.GetTable<SavedEmbed>()
            .InsertWithOutputAsync(() => new SavedEmbed
            {
                GuildId = guildId,
                Name = request.Name,
                EmbedJson = json,
                CreatorId = userId.Value,
            });

        return Ok(new { id = saved.Id, name = saved.Name });
    }

    [HttpDelete("saved/{embedId}")]
    public async Task<IActionResult> DeleteSaved(ulong guildId, int embedId)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<SavedEmbed>()
            .DeleteAsync(x => x.Id == embedId && x.GuildId == guildId);

        return deleted > 0 ? Ok(new { success = true }) : NotFound();
    }
}

public class EmbedModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Author { get; set; }
    public string? Footer { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<EmbedFieldModel>? Fields { get; set; }
}

public class EmbedFieldModel
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Inline { get; set; }
}

public class SaveEmbedRequest
{
    public string Name { get; set; } = "";
    public EmbedModel Embed { get; set; } = new();
}
