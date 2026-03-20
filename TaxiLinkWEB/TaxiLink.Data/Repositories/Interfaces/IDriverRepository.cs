using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Data.Repositories.Interfaces
{
    public interface IDriverRepository : IGenericRepository<Driver>
    {
        Task<Driver?> GetDriverWithDetailsAsync(int id);
    }
}
