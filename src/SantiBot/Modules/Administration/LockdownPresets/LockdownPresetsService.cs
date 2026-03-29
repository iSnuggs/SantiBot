#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Db.Models;
using System.Text.Json;

namespace SantiBot.Modules.Administration.Services;

public sealed class LockdownPresetsService : INService
{
    private readonly DbService _db;

    public LockdownPresetsService(DbService db)
    {
        _db = db;
    }

    public async Task<LockdownPreset> SavePresetAsync(ulong guildId, string name, IGuild guild, ulong userId)
    {
        var channels = await guild.GetTextChannelsAsync();
        var permData = new Dictionary<string, List<PermOverwriteData>>();

        foreach (var ch in channels)
        {
            var overwrites = ch.PermissionOverwrites
                .Select(p => new PermOverwriteData
                {
                    TargetId = p.TargetId,
                    TargetType = p.TargetType.ToString(),
                    AllowValue = p.Permissions.AllowValue,
                    DenyValue = p.Permissions.DenyValue
                }).ToList();

            permData[ch.Id.ToString()] = overwrites;
        }

        var json = JsonSerializer.Serialize(permData);

        await using var ctx = _db.GetDbContext();

        // Delete existing preset with same name
        await ctx.GetTable<LockdownPreset>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync();

        var id = await ctx.GetTable<LockdownPreset>()
            .InsertWithInt32IdentityAsync(() => new LockdownPreset
            {
                GuildId = guildId,
                Name = name,
                PermissionsJson = json,
                CreatedByUserId = userId
            });

        return new LockdownPreset { Id = id, Name = name };
    }

    public async Task<bool> LoadPresetAsync(ulong guildId, string name, IGuild guild)
    {
        await using var ctx = _db.GetDbContext();
        var preset = await ctx.GetTable<LockdownPreset>()
            .FirstOrDefaultAsyncLinqToDB(x => x.GuildId == guildId && x.Name == name);

        if (preset is null) return false;

        var permData = JsonSerializer.Deserialize<Dictionary<string, List<PermOverwriteData>>>(preset.PermissionsJson);
        if (permData is null) return false;

        foreach (var (channelIdStr, overwrites) in permData)
        {
            if (!ulong.TryParse(channelIdStr, out var channelId)) continue;

            var channel = await guild.GetTextChannelAsync(channelId);
            if (channel is null) continue;

            try
            {
                // Remove current overwrites
                foreach (var existing in channel.PermissionOverwrites)
                {
                    if (existing.TargetType == PermissionTarget.Role)
                        await channel.RemovePermissionOverwriteAsync(guild.GetRole(existing.TargetId));
                    else
                        await channel.RemovePermissionOverwriteAsync(await guild.GetUserAsync(existing.TargetId));
                }

                // Apply saved overwrites
                foreach (var ow in overwrites)
                {
                    var perms = new OverwritePermissions(ow.AllowValue, ow.DenyValue);
                    if (ow.TargetType == "Role")
                    {
                        var role = guild.GetRole(ow.TargetId);
                        if (role is not null)
                            await channel.AddPermissionOverwriteAsync(role, perms);
                    }
                    else
                    {
                        var user = await guild.GetUserAsync(ow.TargetId);
                        if (user is not null)
                            await channel.AddPermissionOverwriteAsync(user, perms);
                    }
                }
            }
            catch { /* skip channels we can't modify */ }
        }

        return true;
    }

    public async Task<List<LockdownPreset>> ListPresetsAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<LockdownPreset>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    public async Task<bool> DeletePresetAsync(ulong guildId, string name)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<LockdownPreset>()
            .Where(x => x.GuildId == guildId && x.Name == name)
            .DeleteAsync() > 0;
    }
}

public class PermOverwriteData
{
    public ulong TargetId { get; set; }
    public string TargetType { get; set; }
    public ulong AllowValue { get; set; }
    public ulong DenyValue { get; set; }
}
