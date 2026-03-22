namespace SantiBot.Modules.Music;

public interface IPlatformQueryResolver
{
    Task<ITrackInfo?> ResolveByQueryAsync(string query);
}