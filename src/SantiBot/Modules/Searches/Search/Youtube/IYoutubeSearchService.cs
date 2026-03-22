namespace SantiBot.Modules.Searches.Youtube;

public interface IYoutubeSearchService
{
    Task<VideoInfo[]?> SearchAsync(string query);
}