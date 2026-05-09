using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Services.Interfaces
{
    public interface IGeocodingService
    {
        Task<(string Lat, string Lon)?> GetCoordinatesAsync(string address);
    }
}
