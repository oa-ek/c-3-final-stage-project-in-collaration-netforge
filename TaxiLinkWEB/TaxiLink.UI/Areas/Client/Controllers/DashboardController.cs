using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;

namespace TaxiLink.UI.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class DashboardController : Controller
    {

        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<VehicleClass> _vClassRepo;
        private readonly IGenericRepository<AdditionalService> _serviceRepo;
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<OrderAdditionalService> _orderSrvRepo;

        public DashboardController(
             IGenericRepository<Order> orderRepo,
             IGenericRepository<VehicleClass> vClassRepo,
             IGenericRepository<AdditionalService> serviceRepo,
             IGenericRepository<City> cityRepo,
             IGenericRepository<OrderAdditionalService> orderSrvRepo)
        {
            _orderRepo = orderRepo;
            _vClassRepo = vClassRepo;
            _serviceRepo = serviceRepo;
            _cityRepo = cityRepo;
            _orderSrvRepo = orderSrvRepo;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.VehicleClasses = await _vClassRepo.GetAllAsync();
            return View();
        }

        // КРОК 2: Деталі (Коментар, Послуги)
        [HttpPost]
        public async Task<IActionResult> Confirm(string pickup, string dropoff, decimal distance, int vehicleClassId)
        {
            var vClass = await _vClassRepo.GetByIdAsync(vehicleClassId);

            // Отримуємо Київ (припустимо, його ID = 1)
            var city = await _cityRepo.GetByIdAsync(1);
            decimal multiplier = city?.PriceMultiplier ?? 1.0m;

            // ФОРМУЛА: (Подача + (Км * Ціна за км)) * Коефіцієнт міста
            decimal basePrice = (vClass.BasePrice + (distance * vClass.PricePerKm)) * multiplier;

            ViewBag.Services = await _serviceRepo.GetAllAsync();
            ViewBag.Pickup = pickup;
            ViewBag.Dropoff = dropoff;
            ViewBag.Distance = distance;
            ViewBag.VehicleClassId = vehicleClassId;
            ViewBag.TotalPrice = Math.Round(basePrice, 2);

            return View();
        }

        [HttpPost]
        // ЗМІНЕНО: int[] selectedServices замість List<int>
        public async Task<IActionResult> CreateOrder(string pickup, string dropoff, decimal distance, int vehicleClassId, string comment, int[] selectedServices, decimal finalPrice)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login", "Auth", new { area = "" });

            int userId = int.Parse(userIdClaim);

            var order = new Order
            {
                UserId = userId,
                PickupAddress = pickup,
                DropoffAddress = dropoff,
                Distance = distance,
                VehicleClassId = vehicleClassId,
                ClientComment = comment,
                TotalPrice = finalPrice,
                OrderStatusId = 1,
                PaymentMethodId = 1,
                CityId = 1,
                CreatedAt = DateTime.Now
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            // ЗБЕРІГАЄМО ПОСЛУГИ
            if (selectedServices != null && selectedServices.Length > 0)
            {
                foreach (var srvId in selectedServices)
                {
                    var orderService = new OrderAdditionalService
                    {
                        OrderId = order.Id,
                        AdditionalServiceId = srvId
                    };
                    await _orderSrvRepo.AddAsync(orderService);
                }
                await _orderSrvRepo.SaveChangesAsync();
            }

            return RedirectToAction("Orders", "Dashboard", new { area = "Driver" });
        }
    }
}
