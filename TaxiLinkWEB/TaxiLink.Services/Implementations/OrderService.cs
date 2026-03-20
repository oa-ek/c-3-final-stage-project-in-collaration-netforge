using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;

namespace TaxiLink.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IGenericRepository<Review> _reviewRepo;

        public OrderService(IOrderRepository orderRepo, IGenericRepository<Review> reviewRepo)
        {
            _orderRepo = orderRepo;
            _reviewRepo = reviewRepo;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync() => await _orderRepo.GetAllAsync();

        public async Task<Order?> GetOrderFullDetailsAsync(int id) => await _orderRepo.GetOrderWithFullDetailsAsync(id);

        public async Task<IEnumerable<Order>> GetUserOrderHistoryAsync(int userId) => await _orderRepo.GetOrdersByUserIdAsync(userId);

        public async Task CreateOrderAsync(Order order)
        {
            order.CreatedAt = DateTime.Now;
            order.OrderStatusId = 1; // 1 = Очікується (за замовчуванням для нових)

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, int cancellationReasonId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
            {
                order.OrderStatusId = 4; // 4 = Скасовано
                order.CancellationReasonId = cancellationReasonId;

                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
            }
        }
        public async Task AddReviewAsync(Review review)
        {
            review.CreatedAt = DateTime.Now;
            await _reviewRepo.AddAsync(review);
            await _reviewRepo.SaveChangesAsync();
        }
    }
}
