using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TaxiLink.Data.Context;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Implementations;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Areas.Client.Models;
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
    private readonly IWeatherService _weatherService;
    private readonly IGenericRepository<UserPaymentCard> _cardRepo;
    private readonly IGenericRepository<PromoCode> _promoRepo;
    private readonly DBContextTaxiLink _context;
    private readonly IConfiguration _config;
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
         IWebHostEnvironment webHostEnvironment,
         IWeatherService weatherService,
         IGenericRepository<UserPaymentCard> cardRepo,
         IGenericRepository<PromoCode> promoRepo,
         DBContextTaxiLink context,
         IConfiguration config)
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
        _weatherService = weatherService;
        _cardRepo = cardRepo;
        _promoRepo = promoRepo;
        _context = context;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> PayOrder(int orderId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order == null) return Json(new { success = false, message = "Замовлення не знайдено" });

        var payload = new
        {
            amount = (int)(order.TotalPrice * 100),
            ccy = 980,
            merchantPaymInfo = new
            {
                reference = order.Id.ToString(),
                destination = $"Оплата поїздки TaxiLink #{order.Id}"
            },
           
            redirectUrl = Url.Action("PaymentCallback", "Dashboard", new { area = "Client", orderId = order.Id }, Request.Scheme),
            webHookUrl = Url.Action("MonoWebhook", "Dashboard", new { area = "Client" }, Request.Scheme)
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Token", _config["Monobank:Token"]);
        var response = await client.PostAsJsonAsync("https://api.monobank.ua/api/merchant/invoice/create", payload);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            string pageUrl = result.GetProperty("pageUrl").GetString();

            if (result.TryGetProperty("invoiceId", out JsonElement invoiceIdProp))
            {
                order.ExternalPaymentId = invoiceIdProp.GetString();
                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
            }

            return Json(new { success = true, url = pageUrl });
        }

        return Json(new { success = false, message = "Помилка генерації платежу Monobank" });
    }
    [HttpGet]
    public async Task<IActionResult> PaymentCallback(int orderId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order != null && order.OrderStatusId == 9)
        {
            order.OrderStatusId = 1;
            order.ClientComment += " [ОПЛАЧЕНО]";
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Dashboard", new { area = "Client" });
    }
    [HttpPost]
    public async Task<IActionResult> AddPaymentCard(string cardNumber)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            cardNumber = cardNumber.Replace(" ", "");

            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 16)
                return Json(new { success = false, message = "Невірний номер картки" });
            string mask = "**** **** **** " + cardNumber.Substring(Math.Max(0, cardNumber.Length - 4));

            string system = cardNumber.StartsWith("4") ? "Visa" : "MasterCard";

            var newCard = new UserPaymentCard
            {
                UserId = userId,
                CardMask = mask,
                PaymentSystem = system,
                IsDefault = true
            };

            await _cardRepo.AddAsync(newCard);
            await _cardRepo.SaveChangesAsync();

            return Json(new { success = true, id = newCard.Id, mask = newCard.CardMask, system = newCard.PaymentSystem });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> MonoWebhook([FromBody] dynamic data)
    {
        try
        {
            string status = data.GetProperty("status").GetString();
            string reference = data.GetProperty("reference").GetString();

            if (int.TryParse(reference, out int orderId))
            {
                var order = await _orderRepo.GetByIdAsync(orderId);

                if (order != null && (status == "success" || status == "approved"))
                {
                    if (order.OrderStatusId == 9)
                    {
                        order.OrderStatusId = 1;
                    }

                    order.ClientComment += " [ОПЛАЧЕНО]";
                    _orderRepo.Update(order);
                    await _orderRepo.SaveChangesAsync();
                }
            }
            return Ok();
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> PaymentMethods()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
        int userId = int.Parse(userIdClaim.Value);

        var currentUser = await _userRepo.GetByIdAsync(userId);
        ViewBag.UserCards = await _context.Set<UserPaymentCard>().Where(c => c.UserId == userId).ToListAsync();

        return View(new ClientDashboardViewModel { User = currentUser });
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var currentUser = await _userRepo.GetByIdAsync(userId);

        var allCities = await _cityRepo.GetAllAsync();

        int currentCityId = currentUser.DefaultCityId ?? allCities.FirstOrDefault()?.Id ?? 1;
        var currentCity = await _cityRepo.GetByIdAsync(currentCityId) ?? allCities.FirstOrDefault();
        var coords = await GetCoordinatesAsync(currentCity?.Name ?? "Київ");

        var usdRate = await _currencyService.GetRateAsync("USD") ?? 40.0m;
        var weatherImpact = await _weatherService.GetWeatherImpactAsync(coords.Lat, coords.Lon);

        var allCards = await _cardRepo.GetAllAsync();
        var allPromos = await _promoRepo.GetAllAsync();

        var viewModel = new ClientDashboardViewModel
        {
            User = currentUser,
            VehicleClasses = await _vClassRepo.GetAllAsync(),
            AdditionalServices = await _serviceRepo.GetAllAsync(),
            CityMultiplier = currentCity?.PriceMultiplier ?? 1.0m,
            UsdRate = usdRate,
            WeatherCondition = weatherImpact.ConditionName,
            WeatherMultiplier = weatherImpact.TimeMultiplier,
            PaymentCards = allCards.Where(c => c.UserId == userId).ToList(),
            PromoCodes = allPromos.Where(p => p.ExpiryDate > DateTime.Now && p.CurrentUses < p.MaxUses).ToList(),
            SavedAddresses = await _context.Set<SavedAddress>().Where(a => a.UserId == userId).ToListAsync(),

            AvailableCities = allCities,
            CurrentCity = currentCity
        };

        return View(viewModel);
    }
    [HttpPost]
    public async Task<IActionResult> ChangeCity(int cityId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _userRepo.GetByIdAsync(userId);
        if (user != null)
        {
            user.DefaultCityId = cityId;
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }
    [HttpGet]
    public async Task<IActionResult> SavedAddresses()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
        int userId = int.Parse(userIdClaim.Value);

        var currentUser = await _userRepo.GetByIdAsync(userId);
        ViewBag.SavedAddresses = await _context.Set<SavedAddress>().Where(a => a.UserId == userId).ToListAsync();

        return View(new ClientDashboardViewModel { User = currentUser });
    }

    [HttpPost]
    public async Task<IActionResult> AddSavedAddress(string title, string address)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var newAddress = new SavedAddress { UserId = userId, Title = title, AddressText = address };

        _context.Add(newAddress);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(SavedAddresses));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSavedAddress(int id)
    {
        var address = await _context.Set<SavedAddress>().FindAsync(id);
        if (address != null)
        {
            _context.Remove(address);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(SavedAddresses));
    }

    [HttpGet]
    public async Task<IActionResult> News()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
        int userId = int.Parse(userIdClaim.Value);
        var currentUser = await _userRepo.GetByIdAsync(userId);
        ViewBag.NewsList = await _context.Set<TaxiLink.Domain.Models.NewsItem>()
                                         .OrderByDescending(n => n.PublishedAt)
                                         .ToListAsync();

        var viewModel = new ClientDashboardViewModel
        {
            User = currentUser
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteData(string startLat, string startLon, string endLat, string endLon)
    {
        var routeInfo = await _routingService.GetRouteInfoAsync(startLat, startLon, endLat, endLon);
        if (routeInfo == null) return Json(new { success = false });
        var weatherImpact = await _weatherService.GetWeatherImpactAsync(startLat.Replace(",", "."), startLon.Replace(",", "."));

        return Json(new
        {
            success = true,
            distance = routeInfo.Value.DistanceKm,
            duration = routeInfo.Value.DurationMinutes,
            coordinates = routeInfo.Value.Coordinates,
            weatherCondition = weatherImpact.ConditionName,
            weatherMultiplier = weatherImpact.TimeMultiplier
        });
    }

    [HttpGet]
    public async Task<IActionResult> Support()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });

        int userId = int.Parse(userIdClaim.Value);
        var currentUser = await _userRepo.GetByIdAsync(userId);

        var viewModel = new ClientDashboardViewModel
        {
            User = currentUser
        };

        return View(viewModel);
    }
    private async Task<(string Lat, string Lon)> GetCoordinatesAsync(string cityName)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TaxiLink-App");
            var response = await client.GetAsync($"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(cityName + ", Україна")}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    return (data[0].GetProperty("lat").GetString(), data[0].GetProperty("lon").GetString());
                }
            }
        }
        catch { }
        return ("50.4501", "30.5234"); // Київ за замовчуванням при помилці API
    }

    [HttpGet]
    public async Task<IActionResult> About()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });

        int userId = int.Parse(userIdClaim.Value);
        var currentUser = await _userRepo.GetByIdAsync(userId);

        var viewModel = new ClientDashboardViewModel
        {
            User = currentUser
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
        int userId = int.Parse(userIdClaim.Value);

        var currentUser = await _userRepo.GetByIdAsync(userId);

        var allOrders = await _orderRepo.GetAllAsync();
        var clientOrders = allOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
        ViewBag.OrderStatuses = await _context.OrderStatuses.ToListAsync();
        ViewBag.ClientOrders = clientOrders;
        var viewModel = new ClientDashboardViewModel
        {
            User = currentUser
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveOrder()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var allOrders = await _orderRepo.GetAllAsync();
        var activeOrder = allOrders
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault(o => o.UserId == userId && (o.OrderStatusId < 4 || o.OrderStatusId == 9));

        if (activeOrder != null)
        {
            return Json(new
            {
                success = true,
                orderId = activeOrder.Id,
                statusId = activeOrder.OrderStatusId,
                pickup = activeOrder.PickupAddress, 
                dropoff = activeOrder.DropoffAddress, 
                price = Math.Round(activeOrder.TotalPrice) 
            });
        }
        return Json(new { success = false });
    }

    [HttpGet]
    public async Task<IActionResult> CheckPromoCode(string code)
    {
        var promos = await _promoRepo.GetAllAsync();
        var promo = promos.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase) && p.ExpiryDate > DateTime.Now && p.CurrentUses < p.MaxUses);

        if (promo != null)
            return Json(new { success = true, discountId = promo.Id, discount = promo.DiscountPercentage });

        return Json(new { success = false, message = "Промокод недійсний або його термін дії минув" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequestModel request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _userRepo.GetByIdAsync(userId);
        int initialStatus = request.PaymentMethodId == 2 ? 9 : 1;

        var order = new Order
        {
            UserId = userId,
            PickupAddress = request.Pickup,
            DropoffAddress = request.Dropoff,
            Distance = request.Distance,
            VehicleClassId = request.VehicleClassId,
            ClientComment = request.Comment ?? "",
            TotalPrice = request.FinalPrice,
            OrderStatusId = initialStatus,
            PaymentMethodId = request.PaymentMethodId > 0 ? request.PaymentMethodId : 1,
            PromoCodeId = request.PromoCodeId,
            CityId = user?.DefaultCityId ?? 1,
            CreatedAt = DateTime.Now
        };

        await _orderRepo.AddAsync(order);
        await _orderRepo.SaveChangesAsync();

        if (request.PromoCodeId.HasValue && request.PromoCodeId > 0)
        {
            var promo = await _promoRepo.GetByIdAsync(request.PromoCodeId.Value);
            if (promo != null)
            {
                promo.CurrentUses += 1;
                _promoRepo.Update(promo);
                await _promoRepo.SaveChangesAsync();
            }
        }

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
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var currentUser = await _userRepo.GetByIdAsync(userId);

        if (string.IsNullOrEmpty(currentUser.AvatarPath))
        {
            var googlePicture = User.FindFirst("urn:google:picture")?.Value ?? User.FindFirst("image")?.Value;

            if (!string.IsNullOrEmpty(googlePicture))
            {
                currentUser.AvatarPath = googlePicture;
                _userRepo.Update(currentUser);
                await _userRepo.SaveChangesAsync();
            }
        }

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
    public async Task<IActionResult> CheckOrderStatus(int orderId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order == null) return Json(new { success = false });

        if (order.OrderStatusId > 1 && order.DriverId != null)
        {
            var driver = await _driverRepo.GetByIdAsync(order.DriverId.Value);
            if (driver != null)
            {
                var user = await _userRepo.GetByIdAsync(driver.UserId);

                var vehicle = await _context.Vehicles
                    .Include(v => v.Photos)
                    .FirstOrDefaultAsync(v => v.DriverId == driver.Id);

                string photoUrl = null;
                if (vehicle != null && vehicle.Photos != null && vehicle.Photos.Any())
                {
                    photoUrl = vehicle.Photos.First().PhotoPath;
                }

                return Json(new
                {
                    success = true,
                    statusId = order.OrderStatusId,
                    driverName = user?.FirstName ?? "Водій",
                    driverPhone = user?.PhoneNumber,
                    driverRating = Math.Round(user?.Rating ?? 5.0m, 1),
                    driverAvatar = string.IsNullOrEmpty(user?.AvatarPath) ? null : user.AvatarPath,

                    carBrand = vehicle?.Brand ?? "Марка",
                    carModel = vehicle?.Model ?? "Модель",
                    carColor = vehicle?.Color ?? "Колір",
                    carPlate = vehicle?.LicensePlate ?? "AA 0000 KI",
                    carPhoto = photoUrl
                });
            }
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
    [HttpPost]
    public async Task<IActionResult> SubmitReview(int orderId, int rating, string comment, string isBlocked)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Json(new { success = false });
            int userId = int.Parse(userIdClaim.Value);

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null && order.UserId == userId)
            {
                var review = new TaxiLink.Domain.Models.Review
                {
                    OrderId = order.Id,
                    Rating = rating,
                    Comment = comment ?? "",
                    CreatedAt = DateTime.Now
                };
                _context.Set<TaxiLink.Domain.Models.Review>().Add(review);

                if (order.DriverId.HasValue)
                {
                    var driver = await _driverRepo.GetByIdAsync(order.DriverId.Value);
                    if (driver != null)
                    {
                        var driverUser = await _userRepo.GetByIdAsync(driver.UserId);
                        if (driverUser != null)
                        {
                            driverUser.Rating = (driverUser.Rating + rating) / 2m;
                            _userRepo.Update(driverUser);

                            if (isBlocked == "true")
                            {
                                bool alreadyBlocked = _context.Set<TaxiLink.Domain.Models.Blacklist>()
                                    .Any(b => b.BlockerUserId == userId && b.BlockedUserId == driverUser.Id);

                                if (!alreadyBlocked)
                                {
                                    var blacklistRecord = new TaxiLink.Domain.Models.Blacklist
                                    {
                                        BlockerUserId = userId,      
                                        BlockedUserId = driverUser.Id, 
                                        BlockedAt = DateTime.Now
                                    };
                                    _context.Set<TaxiLink.Domain.Models.Blacklist>().Add(blacklistRecord);
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Замовлення не знайдено." });
        }
        catch (Exception ex)
        {
            string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Json(new { success = false, message = "Помилка БД: " + msg });
        }
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
    public int PaymentMethodId { get; set; }
    public int? PromoCodeId { get; set; }
}
