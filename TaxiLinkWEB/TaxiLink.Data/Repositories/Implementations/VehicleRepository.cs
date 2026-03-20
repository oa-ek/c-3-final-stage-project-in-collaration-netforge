using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Data.Context;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;

namespace TaxiLink.Data.Repositories.Implementations
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(DBContextTaxiLink context) : base(context)
        {
        }

        public async Task<Vehicle?> GetVehicleWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(v => v.Driver)
                    .ThenInclude(d => d.User)
                .Include(v => v.Photos)
                .Include(v => v.VehicleClasses)
                    .ThenInclude(vc => vc.VehicleClass)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByDriverIdAsync(int driverId)
        {
            return await _dbSet
                .Where(v => v.DriverId == driverId)
                .Include(v => v.Photos)
                .ToListAsync();
        }
    }
}
