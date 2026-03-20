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
    public class DriverRepository : GenericRepository<Driver>, IDriverRepository
    {
        public DriverRepository(DBContextTaxiLink context) : base(context)
        {
        }

        public async Task<Driver?> GetDriverWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.User)
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}
