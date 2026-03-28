namespace SantiBot.Modules.Searches;

public partial class Searches
{
    [Group]
    public partial class WeatherWttrCommands : SantiModule<WeatherWttrService>
    {
        [Cmd]
        public async Task Wttr([Leftover] string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                await Response().Error(strs.wttr_no_location).SendAsync();
                return;
            }

            var data = await _service.GetWeatherAsync(location);

            if (data is null)
            {
                await Response().Error(strs.wttr_not_found).SendAsync();
                return;
            }

            var locationName = string.IsNullOrWhiteSpace(data.Region)
                ? $"{data.AreaName}, {data.Country}"
                : $"{data.AreaName}, {data.Region}, {data.Country}";

            var eb = CreateEmbed()
                .WithOkColor()
                .WithTitle($"Weather in {locationName}")
                .WithDescription($"**{data.WeatherDesc}**")
                .AddField("Temperature", $"{data.TempC}°C / {data.TempF}°F", true)
                .AddField("Feels Like", $"{data.FeelsLikeC}°C / {data.FeelsLikeF}°F", true)
                .AddField("Humidity", $"{data.Humidity}%", true)
                .AddField("Wind Speed", $"{data.WindSpeedKmph} km/h", true)
                .WithFooter("Powered by wttr.in");

            await Response().Embed(eb).SendAsync();
        }
    }
}
