using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Services.Interfaces
{
    public interface IRoutingService
    {
        Task<(double DistanceKm, double DurationMinutes, List<List<double>> Coordinates)?> GetRouteInfoAsync(string startLat, string startLon, string endLat, string endLon);
    }
}
