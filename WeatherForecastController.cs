using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/hello")]
public class HelloController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HelloController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetHello([FromQuery] string visitor_name)
    {
        var clientIp = await GetPublicIpAddress();

        if (string.IsNullOrEmpty(clientIp))
        {
            return StatusCode(500, "Could not retrieve IP address");
        }

        var location = await GetLocationData(clientIp);
        if (location.city == null)
        {
            return StatusCode(500, "Could not retrieve location data");
        }
        
        var weather = await GetWeatherData(location.city);

        var response = new
        {
            client_ip = clientIp,
            location = location.city,
            greeting = $"Hello, {visitor_name}!, the temperature is {weather.temp_c} degrees Celsius in {location.city}"
        };

        return Ok(response);
    }

    private async Task<string?> GetPublicIpAddress()
    {
        var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.GetStringAsync("https://api.ipify.org?format=json");
            var json = JObject.Parse(response);

            return json["ip"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<(string city, string region, string country)> GetLocationData(string ip)
    {
        var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.GetStringAsync($"http://ip-api.com/json/{ip}");
            var json = JObject.Parse(response);

            return (
                city: json["city"]?.ToString(),
                region: json["regionName"]?.ToString(),
                country: json["country"]?.ToString()
            );
        }
        catch
        {
            return (null, null, null);
        }
    }

    private async Task<(double temp_c, string condition)> GetWeatherData(string city)
    {
        var apiKey = "1f3141a91890420ba06183003240107";
        var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.GetStringAsync($"http://api.weatherapi.com/v1/current.json?key={apiKey}&q={city}&aqi=no");
            var json = JObject.Parse(response);

            return (
                temp_c: (double)json["current"]["temp_c"],
                condition: json["current"]["condition"]["text"].ToString()
            );
        }
        catch
        {
            return (0, "Unknown");
        }
    }
}
