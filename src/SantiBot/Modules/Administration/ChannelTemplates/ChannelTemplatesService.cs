#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Administration.Services;

public sealed class ChannelTemplatesService : INService
{
    private readonly DbService _db;

    public ChannelTemplatesService(DbService db)
    {
        _db = db;
    }

    public async Task<ChannelTemplate> SaveTemplateAsync(ulong guildId, string name, ITextChannel channel, ulong userId)
    {
        var settings = new ChannelTemplateData
        {
            Topic = channel.Topic ?? "",
            SlowModeInterval = channel.SlowModeInterval,
            IsNsfw = channel.IsNsfw,
            CategoryId = channel.CategoryId,
            PermissionOverwrites = channel.PermissionOverwrites
                .Select(p => new PermTemplateData
                {
                    TargetId = p.TargetId,
                    TargetType = p.TargetType.ToString(),
                    AllowValue = p.Permissions.AllowValue,
                    DenyValue = p.Permissions.DenyValue
                }).ToList()
        };

        var json = JsonSerializer.Serialize(settings);

        await using var ctx = _db.GetDbContext();

        // Replace existing
        await ctx.GetTable<ChannelTemplate>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync();

        var id = await ctx.GetTable<ChannelTemplate>()
            .InsertWithInt32IdentityAsync(() => new ChannelTemplate
            {
                GuildId = guildId,
                Name = name,
                SettingsJson = json,
                CreatedByUserId = userId
            });

        return new ChannelTemplate { Id = id, Name = name };
    }

    public async Task<ITextChannel> LoadTemplateAsync(ulong guildId, string name, IGuild guild, string newName = null)
    {
        await using var ctx = _db.GetDbContext();
        var template = await ctx.GetTable<ChannelTemplate>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);

        if (template is null) return null;

        var settings = JsonSerializer.Deserialize<ChannelTemplateData>(template.SettingsJson);
        if (settings is null) return null;

        var channelName = newName ?? name;
        var channel = await guild.CreateTextChannelAsync(channelName, props =>
        {
            props.Topic = settings.Topic;
            props.SlowModeInterval = settings.SlowModeInterval;
            props.IsNsfw = settings.IsNsfw;
            if (settings.CategoryId.HasValue)
                props.CategoryId = settings.CategoryId;
        });

        // Apply permission overwrites
        foreach (var perm in settings.PermissionOverwrites)
        {
            var perms = new OverwritePermissions(perm.AllowValue, perm.DenyValue);
            try
            {
                if (perm.TargetType == "Role")
                {
                    var role = guild.GetRole(perm.TargetId);
                    if (role is not null)
                        await channel.AddPermissionOverwriteAsync(role, perms);
                }
                else
                {
                    var user = await guild.GetUserAsync(perm.TargetId);
                    if (user is not null)
                        await channel.AddPermissionOverwriteAsync(user, perms);
                }
            }
            catch { }
        }

        return channel;
    }

    public async Task<List<ChannelTemplate>> ListTemplatesAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ChannelTemplate>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeleteTemplateAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ChannelTemplate>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync() > 0;
    }
}

public class ChannelTemplateData
{
    public string Topic { get; set; }
    public int SlowModeInterval { get; set; }
    public bool IsNsfw { get; set; }
    public ulong? CategoryId { get; set; }
    public List<PermTemplateData> PermissionOverwrites { get; set; } = new();
}

public class PermTemplateData
{
    public ulong TargetId { get; set; }
    public string TargetType { get; set; }
    public ulong AllowValue { get; set; }
    public ulong DenyValue { get; set; }
}
