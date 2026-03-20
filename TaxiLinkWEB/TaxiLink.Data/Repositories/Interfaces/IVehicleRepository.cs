using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Data.Repositories.Interfaces
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<Vehicle?> GetVehicleWithDetailsAsync(int id);
        Task<IEnumerable<Vehicle>> GetVehiclesByDriverIdAsync(int driverId);
    }
}
