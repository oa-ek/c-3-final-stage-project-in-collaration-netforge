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
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(DBContextTaxiLink context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Role)
                .Include(u => u.SavedAddress)
                .Include(u => u.PaymentCards)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
