#nullable disable
namespace Santi.Common;

public interface IDiscordPermOverrideService
{
    bool TryGetOverrides(ulong guildId, string commandName, out SantiBot.Db.GuildPerm? perm);
}