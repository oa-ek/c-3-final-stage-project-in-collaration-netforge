using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaxiLink.Domain.DTOs.External;
using TaxiLink.Services.Interfaces;

namespace TaxiLink.Services.Implementations
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(int WeatherCode, double TimeMultiplier, string ConditionName)> GetWeatherImpactAsync(string lat, string lon)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OpenMeteo");
                var response = await client.GetAsync($"/v1/forecast?latitude={lat}&longitude={lon}&current=weather_code");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<OpenMeteoResponseDto>(jsonString);

                    if (data?.Current != null)
                    {
                        return AnalyzeWeatherCode(data.Current.WeatherCode);
                    }
                }
                return (0, 1.0, "Ясно");
            }
            catch
            {
                return (0, 1.0, "Невідомо");
            }
        }

        private (int, double, string) AnalyzeWeatherCode(int code)
        {
            return code switch
            {
                0 or 1 or 2 or 3 => (code, 1.0, "Ясно / Хмарно"),
                45 or 48 or 51 or 53 or 55 => (code, 1.1, "Мряка"),
                61 or 63 or 65 or 80 or 81 or 82 => (code, 1.25, "Дощ"),
                71 or 73 or 75 or 77 or 85 or 86 => (code, 1.4, "Сніг"),
                95 or 96 or 99 => (code, 1.5, "Гроза"),
                _ => (code, 1.0, "Нормальні умови")
            };
        }
    }
}
