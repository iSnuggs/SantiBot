using SantiBot.Modules.Searches;
using System.Text.Json.Serialization;

namespace SantiBot.Services;

public sealed class OfficialGoogleSearchResultEntry : ISearchResultEntry
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = null!;

    [JsonPropertyName("link")]
    public string Url { get; init; } = null!;

    [JsonPropertyName("displayLink")]
    public string DisplayUrl { get; init; } = null!;

    [JsonPropertyName("snippet")]
    public string Description { get; init; } = null!;
}