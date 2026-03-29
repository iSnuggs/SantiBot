#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Administration.Services;

public sealed class ServerBackupService : INService
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;

    public ServerBackupService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task<ServerBackup> CreateBackupAsync(IGuild guild, ulong userId)
    {
        var backup = new ServerBackupData
        {
            GuildName = guild.Name,
            Roles = new List<RoleBackupData>(),
            Channels = new List<ChannelBackupData>(),
            Categories = new List<CategoryBackupData>()
        };

        // Backup roles
        foreach (var role in guild.Roles.Where(r => r.Id != guild.Id && !r.IsManaged).OrderBy(r => r.Position))
        {
            backup.Roles.Add(new RoleBackupData
            {
                Name = role.Name,
                Color = role.Color.RawValue,
                IsHoisted = role.IsHoisted,
                IsMentionable = role.IsMentionable,
                Permissions = role.Permissions.RawValue,
                Position = role.Position
            });
        }

        // Backup categories
        var categories = (await guild.GetCategoriesAsync()).OrderBy(c => c.Position);
        foreach (var cat in categories)
        {
            backup.Categories.Add(new CategoryBackupData
            {
                Name = cat.Name,
                Position = cat.Position,
                Overwrites = cat.PermissionOverwrites.Select(o => new PermBackupData
                {
                    TargetType = o.TargetType.ToString(),
                    TargetName = o.TargetType == PermissionTarget.Role
                        ? guild.GetRole(o.TargetId)?.Name ?? ""
                        : "",
                    AllowValue = o.Permissions.AllowValue,
                    DenyValue = o.Permissions.DenyValue
                }).ToList()
            });
        }

        // Backup channels
        var channels = await guild.GetTextChannelsAsync();
        foreach (var ch in channels.OrderBy(c => c.Position))
        {
            var category = ch.CategoryId.HasValue
                ? (await guild.GetCategoriesAsync()).FirstOrDefault(c => c.Id == ch.CategoryId.Value)?.Name
                : null;

            backup.Channels.Add(new ChannelBackupData
            {
                Name = ch.Name,
                Topic = ch.Topic ?? "",
                IsNsfw = ch.IsNsfw,
                SlowModeInterval = ch.SlowModeInterval,
                CategoryName = category,
                Position = ch.Position
            });
        }

        var json = JsonSerializer.Serialize(backup);

        await using var ctx = _db.GetDbContext();
        var id = await ctx.GetTable<ServerBackup>()
            .InsertWithInt32IdentityAsync(() => new ServerBackup
            {
                GuildId = guild.Id,
                BackupJson = json,
                CreatedByUserId = userId,
                Description = $"Backup of {guild.Name} - {DateTime.UtcNow:g}"
            });

        return new ServerBackup { Id = id, Description = $"Backup created" };
    }

    public async Task<bool> RestoreBackupAsync(IGuild guild, int backupId)
    {
        await using var ctx = _db.GetDbContext();
        var backup = await ctx.GetTable<ServerBackup>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guild.Id && x.Id == backupId);

        if (backup is null) return false;

        var data = JsonSerializer.Deserialize<ServerBackupData>(backup.BackupJson);
        if (data is null) return false;

        // Restore roles (skip existing)
        var existingRoles = guild.Roles.Select(r => r.Name).ToHashSet();
        foreach (var roleData in data.Roles)
        {
            if (existingRoles.Contains(roleData.Name)) continue;
            try
            {
                await guild.CreateRoleAsync(roleData.Name,
                    new GuildPermissions(roleData.Permissions),
                    new Color(roleData.Color),
                    roleData.IsHoisted,
                    roleData.IsMentionable);
            }
            catch { }
        }

        // Restore categories
        var existingCategories = (await guild.GetCategoriesAsync()).Select(c => c.Name).ToHashSet();
        foreach (var catData in data.Categories)
        {
            if (existingCategories.Contains(catData.Name)) continue;
            try
            {
                await guild.CreateCategoryAsync(catData.Name);
            }
            catch { }
        }

        // Restore channels
        var existingChannels = (await guild.GetTextChannelsAsync()).Select(c => c.Name).ToHashSet();
        foreach (var chData in data.Channels)
        {
            if (existingChannels.Contains(chData.Name)) continue;
            try
            {
                var allCats = await guild.GetCategoriesAsync();
                var cat = chData.CategoryName is not null
                    ? allCats.FirstOrDefault(c => c.Name == chData.CategoryName)
                    : null;

                await guild.CreateTextChannelAsync(chData.Name, props =>
                {
                    props.Topic = chData.Topic;
                    props.IsNsfw = chData.IsNsfw;
                    props.SlowModeInterval = chData.SlowModeInterval;
                    if (cat is not null)
                        props.CategoryId = cat.Id;
                });
            }
            catch { }
        }

        return true;
    }

    public async Task<List<ServerBackup>> ListBackupsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<ServerBackup>()
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.DateAdded)
            .ToListAsyncLinqToDB();
    }
}

public class ServerBackupData
{
    public string GuildName { get; set; }
    public List<RoleBackupData> Roles { get; set; }
    public List<ChannelBackupData> Channels { get; set; }
    public List<CategoryBackupData> Categories { get; set; }
}

public class RoleBackupData
{
    public string Name { get; set; }
    public uint Color { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsMentionable { get; set; }
    public ulong Permissions { get; set; }
    public int Position { get; set; }
}

public class ChannelBackupData
{
    public string Name { get; set; }
    public string Topic { get; set; }
    public bool IsNsfw { get; set; }
    public int SlowModeInterval { get; set; }
    public string CategoryName { get; set; }
    public int Position { get; set; }
}

public class CategoryBackupData
{
    public string Name { get; set; }
    public int Position { get; set; }
    public List<PermBackupData> Overwrites { get; set; }
}

public class PermBackupData
{
    public string TargetType { get; set; }
    public string TargetName { get; set; }
    public ulong AllowValue { get; set; }
    public ulong DenyValue { get; set; }
}
