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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(DBContextTaxiLink context) : base(context)
        {
        }

        public async Task<Order?> GetOrderWithFullDetailsAsync(int id)
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.Driver)
                    .ThenInclude(d => d.User)
                .Include(o => o.Vehicle)
                .Include(o => o.OrderStatus)
                .Include(o => o.City)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public override async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.Driver)
                    .ThenInclude(d => d.User)
                .Include(o => o.OrderStatus)
                .Include(o => o.PaymentMethod) 
                .Include(o => o.VehicleClass)  
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderStatus)
                .Include(o => o.Driver)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
