using Microsoft.Extensions.Configuration;
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
    public class RoutingService : IRoutingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RoutingService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<(double DistanceKm, double DurationMinutes, List<List<double>> Coordinates)?> GetRouteInfoAsync(string startLat, string startLon, string endLat, string endLon)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OpenRouteService");
                var apiKey = _configuration["ExternalApis:OpenRouteService:ApiKey"];

                var url = $"/v2/directions/driving-car?api_key={apiKey}&start={startLon},{startLat}&end={endLon},{endLat}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var routeData = JsonSerializer.Deserialize<OrsRouteDto>(jsonString);

                    if (routeData?.Features != null && routeData.Features.Count > 0)
                    {
                        var summary = routeData.Features[0].Properties.Summary;
                        var coordinates = routeData.Features[0].Geometry.Coordinates;

                        double distanceKm = Math.Round(summary.Distance / 1000.0, 1);
                        double durationMin = Math.Round(summary.Duration / 60.0, 0);

                        return (distanceKm, durationMin, coordinates);
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
