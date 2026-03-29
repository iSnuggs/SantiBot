#nullable disable
namespace SantiBot.Modules.Administration.Services;

public sealed class PermissionAuditorService : INService
{
    private readonly DiscordSocketClient _client;

    public PermissionAuditorService(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task<PermAuditResult> AuditGuildAsync(IGuild guild)
    {
        var result = new PermAuditResult();

        // Check roles for dangerous permissions
        foreach (var role in guild.Roles.Where(r => r.Id != guild.Id))
        {
            var dangerous = new List<string>();

            if (role.Permissions.Administrator) dangerous.Add("Administrator");
            if (role.Permissions.BanMembers) dangerous.Add("Ban Members");
            if (role.Permissions.KickMembers) dangerous.Add("Kick Members");
            if (role.Permissions.ManageGuild) dangerous.Add("Manage Server");
            if (role.Permissions.ManageRoles) dangerous.Add("Manage Roles");
            if (role.Permissions.ManageChannels) dangerous.Add("Manage Channels");
            if (role.Permissions.MentionEveryone) dangerous.Add("Mention Everyone");
            if (role.Permissions.ManageWebhooks) dangerous.Add("Manage Webhooks");

            if (dangerous.Count > 0)
            {
                result.DangerousRoles.Add(new PermAuditRoleInfo
                {
                    RoleName = role.Name,
                    RoleId = role.Id,
                    DangerousPerms = dangerous
                });
            }
        }

        // Check @everyone role permissions
        var everyone = guild.EveryoneRole;
        if (everyone.Permissions.MentionEveryone)
            result.Warnings.Add("@everyone role can mention everyone!");
        if (everyone.Permissions.CreateInstantInvite)
            result.Warnings.Add("@everyone role can create invites.");
        if (everyone.Permissions.AddReactions)
        { /* this is normal */ }

        // Check channels for permission conflicts
        var channels = await guild.GetTextChannelsAsync();
        foreach (var ch in channels)
        {
            foreach (var overwrite in ch.PermissionOverwrites)
            {
                // Detect contradicting overwrites (same perm in both allow and deny shouldn't happen via Discord, but check logic)
                if (overwrite.TargetType == PermissionTarget.Role)
                {
                    var role = guild.GetRole(overwrite.TargetId);
                    if (role is null) continue;

                    // Check if role perm allows something but channel denies it
                    if (role.Permissions.SendMessages && overwrite.Permissions.DenyValue != 0)
                    {
                        var denied = new OverwritePermissions(0, overwrite.Permissions.DenyValue);
                        if (denied.SendMessages == PermValue.Deny)
                        {
                            result.Conflicts.Add($"#{ch.Name}: **{role.Name}** has Send Messages granted at role level but denied at channel level");
                        }
                    }
                }
            }
        }

        return result;
    }

    public async Task<PermAuditResult> AuditChannelAsync(ITextChannel channel, IGuild guild)
    {
        var result = new PermAuditResult();

        foreach (var overwrite in channel.PermissionOverwrites)
        {
            string targetName;
            if (overwrite.TargetType == PermissionTarget.Role)
            {
                var role = guild.GetRole(overwrite.TargetId);
                targetName = role?.Name ?? "Unknown Role";
            }
            else
            {
                var user = await guild.GetUserAsync(overwrite.TargetId);
                targetName = user?.Username ?? "Unknown User";
            }

            var allows = new List<string>();
            var denies = new List<string>();

            foreach (var perm in Enum.GetValues<ChannelPermission>())
            {
                if (overwrite.Permissions.ToAllowList().Contains(perm))
                    allows.Add(perm.ToString());
                if (overwrite.Permissions.ToDenyList().Contains(perm))
                    denies.Add(perm.ToString());
            }

            if (allows.Count > 0 || denies.Count > 0)
            {
                result.ChannelOverwrites.Add(new PermAuditOverwrite
                {
                    TargetName = targetName,
                    TargetType = overwrite.TargetType.ToString(),
                    Allows = allows,
                    Denies = denies
                });
            }
        }

        return result;
    }

    public PermAuditResult AuditRole(IRole role, IGuild guild)
    {
        var result = new PermAuditResult();

        var perms = new List<string>();
        foreach (var perm in Enum.GetValues<GuildPermission>())
        {
            if (role.Permissions.Has(perm))
                perms.Add(perm.ToString());
        }

        result.RolePermissions = perms;
        result.RoleName = role.Name;

        return result;
    }
}

public class PermAuditResult
{
    public List<PermAuditRoleInfo> DangerousRoles { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
    public List<PermAuditOverwrite> ChannelOverwrites { get; set; } = new();
    public List<string> RolePermissions { get; set; } = new();
    public string RoleName { get; set; }
}

public class PermAuditRoleInfo
{
    public string RoleName { get; set; }
    public ulong RoleId { get; set; }
    public List<string> DangerousPerms { get; set; }
}

public class PermAuditOverwrite
{
    public string TargetName { get; set; }
    public string TargetType { get; set; }
    public List<string> Allows { get; set; }
    public List<string> Denies { get; set; }
}
