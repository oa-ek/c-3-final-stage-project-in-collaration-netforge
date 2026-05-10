using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<(int WeatherCode, double TimeMultiplier, string ConditionName)> GetWeatherImpactAsync(string lat, string lon);
    }
}
