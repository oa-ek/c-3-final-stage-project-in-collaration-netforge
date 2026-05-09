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
    public class GeocodingService : IGeocodingService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GeocodingService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(string Lat, string Lon)?> GetCoordinatesAsync(string address)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("Nominatim");
                var response = await client.GetAsync($"/search?q={Uri.EscapeDataString(address)}&format=json&limit=1");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<List<NominatimResponseDto>>(jsonString);

                    if (results != null && results.Count > 0)
                    {
                        return (results[0].Lat, results[0].Lon);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
