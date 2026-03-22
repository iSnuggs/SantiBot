using SantiBot.Modules.Searches;
using System.Text.Json.Serialization;

namespace SantiBot.Services;

public sealed class GoogleImageResultEntry : IImageSearchResultEntry
{
    [JsonPropertyName("link")]
    public string Link { get; init; } = null!;

    [JsonPropertyName("image")]
    public GoogleImageData Image { get; init; } = null!;
}