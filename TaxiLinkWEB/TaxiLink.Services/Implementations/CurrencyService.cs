using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaxiLink.Domain.DTOs.External;
using TaxiLink.Services.Interfaces;
using System.Net.Http;

namespace TaxiLink.Services.Implementations
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public CurrencyService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<decimal?> GetRateAsync(string currencyCode)
        {
            string cacheKey = $"NbuRate_{currencyCode.ToUpper()}";

            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                return cachedRate;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("NBU");
                var response = await client.GetAsync($"/NBUStatService/v1/statdirectory/exchange?valcode={currencyCode}&json");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var rates = JsonSerializer.Deserialize<List<NbuRateDto>>(jsonString);

                    if (rates != null && rates.Count > 0)
                    {
                        var rate = rates[0].Rate;
                        _cache.Set(cacheKey, rate, TimeSpan.FromHours(12));
                        return rate;
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
