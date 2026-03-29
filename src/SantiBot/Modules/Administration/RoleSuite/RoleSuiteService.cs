#nullable disable
namespace SantiBot.Modules.Administration.Services;

public sealed class RoleSuiteService : INService
{
    private readonly DiscordSocketClient _client;

    public RoleSuiteService(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task<IRole> CreateRoleAsync(IGuild guild, string name, string colorHex)
    {
        Color? color = null;
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            colorHex = colorHex.TrimStart('#');
            if (uint.TryParse(colorHex, System.Globalization.NumberStyles.HexNumber, null, out var rawColor))
                color = new Color(rawColor);
        }

        return await guild.CreateRoleAsync(name, color: color, isMentionable: false, isHoisted: false);
    }

    public async Task<bool> DeleteRoleAsync(IGuild guild, IRole role)
    {
        try
        {
            await role.DeleteAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RecolorRoleAsync(IRole role, string colorHex)
    {
        try
        {
            colorHex = colorHex.TrimStart('#');
            if (!uint.TryParse(colorHex, System.Globalization.NumberStyles.HexNumber, null, out var rawColor))
                return false;

            await role.ModifyAsync(r => r.Color = new Color(rawColor));
            return true;
        }
        catch { return false; }
    }

    public RoleInfoData GetRoleInfo(IRole role, IGuild guild)
    {
        return new RoleInfoData
        {
            Name = role.Name,
            Color = role.Color.RawValue == 0 ? "Default" : $"#{role.Color.RawValue:X6}",
            Position = role.Position,
            IsHoisted = role.IsHoisted,
            IsMentionable = role.IsMentionable,
            IsManaged = role.IsManaged,
            Permissions = role.Permissions,
            CreatedAt = role.CreatedAt,
            Id = role.Id
        };
    }

    public async Task<List<IGuildUser>> GetRoleMembersAsync(IGuild guild, IRole role)
    {
        var users = await guild.GetUsersAsync();
        return users.Where(u => u.RoleIds.Contains(role.Id)).ToList();
    }
}

public class RoleInfoData
{
    public string Name { get; set; }
    public string Color { get; set; }
    public int Position { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsMentionable { get; set; }
    public bool IsManaged { get; set; }
    public GuildPermissions Permissions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ulong Id { get; set; }
}
