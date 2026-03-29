#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Utility.DashboardApi.EmbedBuilderV2;

/// <summary>
/// Enhanced embed creation with template library.
/// API: GET/POST /api/guild/{guildId}/embeds/templates
/// Pre-built templates: welcome, rules, announcement, info, faq
/// </summary>
public sealed class EmbedTemplateService : INService
{
    private readonly DbService _db;

    private static readonly Dictionary<string, string> BuiltInTemplates = new()
    {
        ["welcome"] = JsonSerializer.Serialize(new
        {
            title = "Welcome to {server}!",
            description = "Hey {user}, welcome to **{server}**! Please read the rules and have fun!",
            color = "#43b581",
            thumbnail = "{server_icon}",
            footer = "Member #{member_count}"
        }),
        ["rules"] = JsonSerializer.Serialize(new
        {
            title = "Server Rules",
            description = "1. Be respectful\n2. No spam\n3. No NSFW\n4. Follow Discord ToS\n5. Have fun!",
            color = "#faa61a",
            footer = "Breaking rules may result in a mute or ban"
        }),
        ["announcement"] = JsonSerializer.Serialize(new
        {
            title = "Announcement",
            description = "Enter your announcement here...",
            color = "#5865f2",
            timestamp = true
        }),
        ["info"] = JsonSerializer.Serialize(new
        {
            title = "Server Information",
            description = "**Server:** {server}\n**Members:** {member_count}\n**Created:** {server_created}",
            color = "#5865f2",
            thumbnail = "{server_icon}"
        }),
        ["faq"] = JsonSerializer.Serialize(new
        {
            title = "Frequently Asked Questions",
            description = "**Q: How do I get roles?**\nA: Use the role menu in #roles\n\n**Q: How do I level up?**\nA: Chat and be active!",
            color = "#e91e63"
        })
    };

    public EmbedTemplateService(DbService db)
    {
        _db = db;
    }

    public Dictionary<string, string> GetBuiltInTemplates()
        => BuiltInTemplates;

    public async Task<int> SaveTemplateAsync(ulong guildId, string name, string category, string embedJson)
    {
        await using var uow = _db.GetDbContext();
        // Check if exists, update instead
        var existing = await uow.GetTable<EmbedTemplate>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);

        if (existing is not null)
        {
            await uow.GetTable<EmbedTemplate>()
                .Where(x => x.Id == existing.Id)
                .UpdateAsync(x => new EmbedTemplate
                {
                    EmbedJson = embedJson,
                    Category = category
                });
            return existing.Id;
        }

        var template = await uow.GetTable<EmbedTemplate>()
            .InsertWithOutputAsync(() => new EmbedTemplate
            {
                GuildId = guildId,
                Name = name,
                Category = category,
                EmbedJson = embedJson
            });
        return template.Id;
    }

    public async Task<List<EmbedTemplate>> ListTemplatesAsync(ulong guildId)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<EmbedTemplate>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<EmbedTemplate> GetTemplateAsync(ulong guildId, string name)
    {
        await using var uow = _db.GetDbContext();
        return await uow.GetTable<EmbedTemplate>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);
    }

    public async Task<bool> DeleteTemplateAsync(ulong guildId, string name)
    {
        await using var uow = _db.GetDbContext();
        var count = await uow.GetTable<EmbedTemplate>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync();
        return count > 0;
    }

    public EmbedBuilder BuildFromJson(string embedJson, IGuild guild = null, IUser user = null)
    {
        using var doc = JsonDocument.Parse(embedJson);
        var root = doc.RootElement;

        var embed = new EmbedBuilder();

        if (root.TryGetProperty("title", out var title))
            embed.WithTitle(ReplacePlaceholders(title.GetString(), guild, user));
        if (root.TryGetProperty("description", out var desc))
            embed.WithDescription(ReplacePlaceholders(desc.GetString(), guild, user));
        if (root.TryGetProperty("color", out var color))
        {
            var hex = color.GetString().TrimStart('#');
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var colorVal))
                embed.WithColor(new Discord.Color(colorVal));
        }
        if (root.TryGetProperty("thumbnail", out var thumb))
        {
            var thumbUrl = ReplacePlaceholders(thumb.GetString(), guild, user);
            if (Uri.IsWellFormedUriString(thumbUrl, UriKind.Absolute))
                embed.WithThumbnailUrl(thumbUrl);
        }
        if (root.TryGetProperty("footer", out var footer))
            embed.WithFooter(ReplacePlaceholders(footer.GetString(), guild, user));
        if (root.TryGetProperty("timestamp", out var ts) && ts.GetBoolean())
            embed.WithCurrentTimestamp();

        return embed;
    }

    private static string ReplacePlaceholders(string input, IGuild guild, IUser user)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (guild is not null)
        {
            input = input
                .Replace("{server}", guild.Name)
                .Replace("{server_icon}", guild.IconUrl ?? "")
                .Replace("{member_count}", (guild as SocketGuild)?.MemberCount.ToString() ?? "?");
        }
        if (user is not null)
        {
            input = input
                .Replace("{user}", user.Mention)
                .Replace("{username}", user.Username);
        }
        return input;
    }
}
