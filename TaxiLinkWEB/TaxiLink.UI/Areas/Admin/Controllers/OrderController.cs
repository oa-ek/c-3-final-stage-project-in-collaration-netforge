using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Admin_areas.Models;
using static TaxiLink.UI.Admin_areas.Models.AdminViewModels;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{

    private readonly IOrderService _orderService;
    private readonly IUserService _userService;
    private readonly IDriverService _driverService;
    private readonly IGenericRepository<City> _cityRepo;
    private readonly IGenericRepository<VehicleClass> _vClassRepo;
    private readonly IGenericRepository<PaymentMethod> _payRepo;
    private readonly IGenericRepository<AdditionalService> _serviceRepo;
    private readonly IGenericRepository<OrderStatus> _statusRepo;
    private readonly IGenericRepository<Order> _orderBaseRepo;
    private readonly IGenericRepository<OrderAdditionalService> _orderServicesLinkRepo; 

    public OrderController(
        IOrderService orderService,
        IUserService userService,
        IDriverService driverService,
        IGenericRepository<City> cityRepo,
        IGenericRepository<VehicleClass> vClassRepo,
        IGenericRepository<PaymentMethod> payRepo,
        IGenericRepository<AdditionalService> serviceRepo,
        IGenericRepository<OrderStatus> statusRepo,
        IGenericRepository<Order> orderBaseRepo,
        IGenericRepository<OrderAdditionalService> orderServicesLinkRepo)
    {
        _orderService = orderService;
        _userService = userService;
        _driverService = driverService;
        _cityRepo = cityRepo;
        _vClassRepo = vClassRepo;
        _payRepo = payRepo;
        _serviceRepo = serviceRepo;
        _statusRepo = statusRepo;
        _orderBaseRepo = orderBaseRepo;
        _orderServicesLinkRepo = orderServicesLinkRepo;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminViewModels.OrderPageViewModel
        {
            Orders = await _orderService.GetAllOrdersAsync(),
            Drivers = new SelectList(await _driverService.GetAllDriversAsync(), "Id", "User.FirstName"),
            Cities = new SelectList(await _cityRepo.GetAllAsync(), "Id", "Name"),
            VehicleClasses = new SelectList(await _vClassRepo.GetAllAsync(), "Id", "Name"),
            PaymentMethods = new SelectList(await _payRepo.GetAllAsync(), "Id", "Name"),
            Statuses = new SelectList(await _statusRepo.GetAllAsync(), "Id", "Name"),
            AvailableServices = await _serviceRepo.GetAllAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetails(string phone)
    {
        var users = await _userService.GetAllUsersAsync();
        var user = users.FirstOrDefault(u => u.PhoneNumber == phone);
        return Json(user != null ? new { exists = true, fullName = $"{user.FirstName} {user.LastName}", id = user.Id } : new { exists = false });
    }

    [HttpGet]
    public async Task<IActionResult> GetCityMultiplier(int id)
    {
        var city = await _cityRepo.GetByIdAsync(id);
        return Json(new { multiplier = city?.PriceMultiplier ?? 1m });
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await _orderService.GetOrderFullDetailsAsync(id);
        if (order == null) return NotFound();

        var city = await _cityRepo.GetByIdAsync(order.CityId);
        var allLinks = await _orderServicesLinkRepo.GetAllAsync();
        var serviceIds = allLinks.Where(s => s.OrderId == id).Select(s => s.AdditionalServiceId).ToList();

        return Json(new
        {
            order = new
            {
                id = order.Id,
                userId = order.UserId,
                passengerPhone = order.PassengerPhone,
                passengerName = order.PassengerName,
                driverId = order.DriverId,
                vehicleClassId = order.VehicleClassId,
                cityId = order.CityId,
                pickupAddress = order.PickupAddress,
                dropoffAddress = order.DropoffAddress,
                distance = order.Distance,
                paymentMethodId = order.PaymentMethodId,
                clientPriceBonus = order.ClientPriceBonus,
                totalPrice = order.TotalPrice,
                orderStatusId = order.OrderStatusId,
                clientComment = order.ClientComment,
                cityMultiplier = city?.PriceMultiplier ?? 1m
            },
            serviceIds = serviceIds 
        });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(Order order, int[] SelectedServiceIds)
    {
        if (order.UserId == 0)
        {
            var anyUser = (await _userService.GetAllUsersAsync()).FirstOrDefault();
            if (anyUser != null) order.UserId = anyUser.Id;
        }
        decimal cityMultiplier = 1m;
        var city = await _cityRepo.GetByIdAsync(order.CityId);
        if (city != null) cityMultiplier = city.PriceMultiplier;

        decimal servicesPrice = 0;
        if (SelectedServiceIds != null && SelectedServiceIds.Any())
        {
            var allServices = await _serviceRepo.GetAllAsync();
            servicesPrice = allServices.Where(s => SelectedServiceIds.Contains(s.Id)).Sum(s => s.Price);
        }

        order.TotalPrice = (order.Distance * 15m * cityMultiplier) + order.ClientPriceBonus + servicesPrice;

        if (order.Id == 0)
        {
            order.CreatedAt = DateTime.Now;
            await _orderService.CreateOrderAsync(order);
            await SaveOrderServicesAsync(order.Id, SelectedServiceIds); 
        }
        else
        {
            await _orderService.UpdateOrderAsync(order);
            await SaveOrderServicesAsync(order.Id, SelectedServiceIds); 
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _orderBaseRepo.GetByIdAsync(id);
        if (order != null)
        {
            var links = (await _orderServicesLinkRepo.GetAllAsync()).Where(s => s.OrderId == id).ToList();
            foreach (var l in links) _orderServicesLinkRepo.Delete(l);
            await _orderServicesLinkRepo.SaveChangesAsync();

            _orderBaseRepo.Delete(order);
            await _orderBaseRepo.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    private async Task SaveOrderServicesAsync(int orderId, int[] serviceIds)
    {
        var oldServices = (await _orderServicesLinkRepo.GetAllAsync()).Where(s => s.OrderId == orderId).ToList();
        foreach (var s in oldServices) _orderServicesLinkRepo.Delete(s);
        await _orderServicesLinkRepo.SaveChangesAsync();

        if (serviceIds != null && serviceIds.Any())
        {
            foreach (var sid in serviceIds)
            {
                await _orderServicesLinkRepo.AddAsync(new OrderAdditionalService { OrderId = orderId, AdditionalServiceId = sid });
            }
            await _orderServicesLinkRepo.SaveChangesAsync();
        }
    }
}