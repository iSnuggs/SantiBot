using MorseCode.ITask;

namespace SantiBot.Modules.Searches;

public interface ISearchService
{
    ITask<ISearchResult?> SearchAsync(string? query);
    ITask<IImageSearchResult?> SearchImagesAsync(string query);
}