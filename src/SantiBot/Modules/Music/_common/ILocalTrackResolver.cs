#nullable disable
namespace SantiBot.Modules.Music;

public interface ILocalTrackResolver : IPlatformQueryResolver
{
    IAsyncEnumerable<ITrackInfo> ResolveDirectoryAsync(string dirPath);
}