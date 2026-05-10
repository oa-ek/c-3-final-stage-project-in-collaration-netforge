using Microsoft.AspNetCore.Mvc;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;

namespace TaxiLink.UI.Controllers.Api
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;

        public OrdersApiController(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderRepo.GetAllAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            order.CreatedAt = DateTime.Now;
            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (id != order.Id) return BadRequest();
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            _orderRepo.Delete(order);
            await _orderRepo.SaveChangesAsync();
            return NoContent();
        }
    }
}
