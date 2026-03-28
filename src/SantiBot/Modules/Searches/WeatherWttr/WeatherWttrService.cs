using System.Net.Http;
using System.Text.Json;

namespace SantiBot.Modules.Searches;

public sealed class WeatherWttrService : INService
{
    private readonly IHttpClientFactory _httpFactory;

    public WeatherWttrService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<WttrData?> GetWeatherAsync(string location)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("User-Agent", "SantiBot/1.0");

            var encoded = Uri.EscapeDataString(location);
            var url = $"https://wttr.in/{encoded}?format=j1";
            var response = await http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            var root = json.RootElement;

            var current = root.GetProperty("current_condition")[0];
            var nearestArea = root.GetProperty("nearest_area")[0];

            var areaName = nearestArea.GetProperty("areaName")[0].GetProperty("value").GetString() ?? location;
            var country = nearestArea.GetProperty("country")[0].GetProperty("value").GetString() ?? "";
            var region = nearestArea.GetProperty("region")[0].GetProperty("value").GetString() ?? "";

            var weatherDesc = current.GetProperty("weatherDesc")[0].GetProperty("value").GetString() ?? "Unknown";
            var tempC = current.GetProperty("temp_C").GetString() ?? "0";
            var tempF = current.GetProperty("temp_F").GetString() ?? "0";
            var humidity = current.GetProperty("humidity").GetString() ?? "0";
            var windSpeedKmph = current.GetProperty("windspeedKmph").GetString() ?? "0";
            var feelsLikeC = current.GetProperty("FeelsLikeC").GetString() ?? "0";
            var feelsLikeF = current.GetProperty("FeelsLikeF").GetString() ?? "0";

            return new WttrData
            {
                AreaName = areaName,
                Country = country,
                Region = region,
                WeatherDesc = weatherDesc,
                TempC = tempC,
                TempF = tempF,
                Humidity = humidity,
                WindSpeedKmph = windSpeedKmph,
                FeelsLikeC = feelsLikeC,
                FeelsLikeF = feelsLikeF
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error fetching weather from wttr.in for {Location}", location);
            return null;
        }
    }
}

public class WttrData
{
    public string AreaName { get; set; } = "";
    public string Country { get; set; } = "";
    public string Region { get; set; } = "";
    public string WeatherDesc { get; set; } = "";
    public string TempC { get; set; } = "";
    public string TempF { get; set; } = "";
    public string Humidity { get; set; } = "";
    public string WindSpeedKmph { get; set; } = "";
    public string FeelsLikeC { get; set; } = "";
    public string FeelsLikeF { get; set; } = "";
}
