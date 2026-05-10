using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Implementations;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Areas.Client.Models;

namespace TaxiLink.UI.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IRoutingService _routingService;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<VehicleClass> _vClassRepo;
        private readonly IGenericRepository<AdditionalService> _serviceRepo;
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<OrderAdditionalService> _orderSrvRepo;
        private readonly IUserRepository _userRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly ICurrencyService _currencyService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
             IGenericRepository<Order> orderRepo,
             IGenericRepository<VehicleClass> vClassRepo,
             IGenericRepository<AdditionalService> serviceRepo,
             IGenericRepository<City> cityRepo,
             IGenericRepository<OrderAdditionalService> orderSrvRepo,
             IUserRepository userRepo,
             IDriverRepository driverRepo,
             IVehicleRepository vehicleRepo,
             IRoutingService routingService,
             ICurrencyService currencyService,
             IWebHostEnvironment webHostEnvironment)
        {
            _orderRepo = orderRepo;
            _vClassRepo = vClassRepo;
            _serviceRepo = serviceRepo;
            _cityRepo = cityRepo;
            _orderSrvRepo = orderSrvRepo;
            _userRepo = userRepo;
            _driverRepo = driverRepo;
            _vehicleRepo = vehicleRepo;
            _routingService = routingService;
            _currencyService = currencyService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUser = await _userRepo.GetByIdAsync(userId);
            var city = await _cityRepo.GetByIdAsync(1);
            var usdRate = await _currencyService.GetRateAsync("USD") ?? 40.0m;

            var viewModel = new ClientDashboardViewModel
            {
                User = currentUser,
                VehicleClasses = await _vClassRepo.GetAllAsync(),
                AdditionalServices = await _serviceRepo.GetAllAsync(),
                CityMultiplier = city?.PriceMultiplier ?? 1.0m,
                UsdRate = usdRate
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUser = await _userRepo.GetByIdAsync(userId);

            var viewModel = new ClientDashboardViewModel
            {
                User = currentUser,
                VehicleClasses = new List<VehicleClass>(),
                AdditionalServices = new List<AdditionalService>(),
                CityMultiplier = 1.0m,
                UsdRate = 40.0m
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserProfileEditModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userRepo.GetByIdAsync(userId);

            if (user == null) return NotFound();

            user.FirstName = !string.IsNullOrWhiteSpace(model.FirstName) ? model.FirstName : user.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber : user.PhoneNumber;
            user.Email = model.Email;
            user.PrefersSilentRide = model.PrefersSilentRide;
            user.PrefersNoMusic = model.PrefersNoMusic;

            if (model.AvatarUpload != null && model.AvatarUpload.Length > 0)
            {
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "avatars");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarUpload.FileName);
                using (var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.AvatarUpload.CopyToAsync(fs);
                }
                user.AvatarPath = "/img/avatars/" + fileName;
            }

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> GetRouteFromCoords(string startLat, string startLon, string endLat, string endLon)
        {
            var routeInfo = await _routingService.GetRouteInfoAsync(startLat, startLon, endLat, endLon);
            if (routeInfo == null) return Json(new { success = false });

            return Json(new { success = true, distance = routeInfo.Value.DistanceKm, duration = routeInfo.Value.DurationMinutes, coordinates = routeInfo.Value.Coordinates });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestModel request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var order = new Order
            {
                UserId = userId,
                PickupAddress = request.Pickup,
                DropoffAddress = request.Dropoff,
                Distance = request.Distance,
                VehicleClassId = request.VehicleClassId,
                ClientComment = request.Comment ?? "",
                TotalPrice = request.FinalPrice,
                OrderStatusId = 1,
                PaymentMethodId = 1,
                CityId = 1,
                CreatedAt = DateTime.Now
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            if (request.SelectedServices != null && request.SelectedServices.Any())
            {
                foreach (var srvId in request.SelectedServices)
                {
                    await _orderSrvRepo.AddAsync(new OrderAdditionalService { OrderId = order.Id, AdditionalServiceId = srvId });
                }
                await _orderSrvRepo.SaveChangesAsync();
            }

            return Json(new { success = true, orderId = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> CheckOrderStatus(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return Json(new { success = false });

            if (order.OrderStatusId > 1 && order.DriverId != null)
            {
                var driver = await _driverRepo.GetByIdAsync(order.DriverId.Value);
                var user = await _userRepo.GetByIdAsync(driver.UserId);
                var vehicles = await _vehicleRepo.GetAllAsync();
                var vehicle = vehicles.FirstOrDefault(v => v.DriverId == driver.Id);

                return Json(new
                {
                    success = true,
                    statusId = order.OrderStatusId,
                    driverName = user?.FirstName ?? "Водій",
                    driverRating = user?.Rating ?? 5.0m,
                    driverAvatar = string.IsNullOrEmpty(user?.AvatarPath) ? null : user.AvatarPath,
                    carBrand = vehicle?.Brand ?? "Автомобіль",
                    carModel = vehicle?.Model ?? "",
                    carColor = vehicle?.Color ?? "Не вказано",
                    carPlate = vehicle?.LicensePlate ?? "AA0000"
                });
            }

            return Json(new { success = true, statusId = order.OrderStatusId });
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
            {
                order.OrderStatusId = 5;
                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }

    public class OrderRequestModel
    {
        public string Pickup { get; set; }
        public string Dropoff { get; set; }
        public decimal Distance { get; set; }
        public int VehicleClassId { get; set; }
        public string Comment { get; set; }
        public decimal FinalPrice { get; set; }
        public int[] SelectedServices { get; set; }
    }
}
