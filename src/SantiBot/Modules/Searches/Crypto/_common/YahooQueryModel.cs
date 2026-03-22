using System.Text.Json.Serialization;

namespace SantiBot.Modules.Searches;

public class YahooQueryModel
{
    [JsonPropertyName("quoteResponse")]
    public QuoteResponse QuoteResponse { get; set; } = null!;
}