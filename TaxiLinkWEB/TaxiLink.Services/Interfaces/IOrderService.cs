using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Domain.Models;

namespace TaxiLink.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderFullDetailsAsync(int id);
        Task<IEnumerable<Order>> GetUserOrderHistoryAsync(int userId);
        Task CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task CancelOrderAsync(int orderId, int cancellationReasonId);

        // Відгуки
        Task AddReviewAsync(Review review);
    }
}
