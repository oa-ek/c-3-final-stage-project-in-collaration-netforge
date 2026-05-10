using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaxiLink.Domain.DTOs.External
{
    public class OpenMeteoResponseDto
    {
        [JsonPropertyName("current")]
        public CurrentWeatherDto Current { get; set; }
    }

    public class CurrentWeatherDto
    {
        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}
