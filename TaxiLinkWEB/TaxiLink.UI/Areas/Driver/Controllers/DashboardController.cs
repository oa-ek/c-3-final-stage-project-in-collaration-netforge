using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Areas.Driver.Models;

namespace TaxiLink.UI.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize(Roles = "Driver")]
    public class DashboardController : DriverBaseController
    {
        private readonly IDriverRepository _driverRepo;
        private readonly IUserRepository _userRepo;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly IGenericRepository<AdditionalService> _serviceRepo;
        private readonly IGenericRepository<VehicleAdditionalService> _vServiceRepo;
        private readonly IGenericRepository<VehicleClass> _vClassRepo;
        private readonly IGenericRepository<VehicleVehicleClass> _vVehicleClassRepo;
        private readonly IGenericRepository<VehiclePhoto> _vPhotoRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOrderRepository _orderRepo;
        private readonly IGenericRepository<OrderStatus> _statusRepo;
        private readonly IGenericRepository<CancellationReason> _cancelReasonRepo;
        private readonly IGenericRepository<OrderAdditionalService> _orderSrvRepo;
        private readonly IGenericRepository<NewsItem> _newsRepo;
        private readonly IGeocodingService _geocodingService;
        private readonly IRoutingService _routingService;
        private readonly ICurrencyService _currencyService;
        private readonly IWeatherService _weatherService;

        public DashboardController(
            IDriverService driverService,
            IDriverRepository driverRepo,
            IUserRepository userRepo,
            IVehicleRepository vehicleRepo,
            IGenericRepository<AdditionalService> serviceRepo,
            IGenericRepository<VehicleAdditionalService> vServiceRepo,
            IGenericRepository<VehicleClass> vClassRepo,
            IGenericRepository<VehicleVehicleClass> vVehicleClassRepo,
            IGenericRepository<VehiclePhoto> vPhotoRepo,
            IWebHostEnvironment webHostEnvironment,
            IOrderRepository orderRepo,
            IGenericRepository<OrderStatus> statusRepo,
            IGenericRepository<CancellationReason> cancelReasonRepo,
            IGenericRepository<OrderAdditionalService> orderSrvRepo,
            IGenericRepository<NewsItem> newsRepo,
            IGeocodingService geocodingService,
            IRoutingService routingService,
            ICurrencyService currencyService,
            IWeatherService weatherService)
            : base(driverService)
        {
            _driverRepo = driverRepo;
            _userRepo = userRepo;
            _vehicleRepo = vehicleRepo;
            _serviceRepo = serviceRepo;
            _vServiceRepo = vServiceRepo;
            _vClassRepo = vClassRepo;
            _vVehicleClassRepo = vVehicleClassRepo;
            _vPhotoRepo = vPhotoRepo;
            _webHostEnvironment = webHostEnvironment;
            _orderRepo = orderRepo;
            _statusRepo = statusRepo;
            _cancelReasonRepo = cancelReasonRepo;
            _orderSrvRepo = orderSrvRepo;
            _newsRepo = newsRepo;
            _geocodingService = geocodingService;
            _routingService = routingService;
            _currencyService = currencyService;
            _weatherService = weatherService;
        }

        private void SetLayoutData(TaxiLink.Domain.Models.Driver d)
        {
            ViewBag.IsWorkingMode = d.IsWorkingMode;
            ViewBag.WalletBalance = d.WalletBalance;
            ViewBag.Rating = d.User?.Rating ?? 5.0m;
            ViewBag.IsVerified = d.IsVerified;
            ViewBag.AvatarPath = d.User?.AvatarPath;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });
            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            SetLayoutData(details);
            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
            int userId = int.Parse(userIdClaim.Value);

            var currentUser = await _userRepo.GetByIdAsync(userId);
            var allDrivers = await _driverRepo.GetAllAsync();
            var driver = allDrivers.FirstOrDefault(d => d.UserId == userId);

            var model = new DriverProfileViewModel
            {
                FirstName = currentUser?.FirstName,
                LastName = currentUser?.LastName,
                Email = currentUser?.Email,
                PhoneNumber = currentUser?.PhoneNumber,
                AvatarPath = currentUser?.AvatarPath,
                Rating = currentUser?.Rating ?? 5.0m,
                Patronymic = driver?.Patronymic,
                DateOfBirth = driver?.DateOfBirth,
                TaxId = driver?.TaxId,
                Iban = driver?.Iban,
                IsFopActive = driver?.IsFopActive ?? false,
                IsVerified = driver?.IsVerified ?? false,
                AcceptanceRate = driver?.AcceptanceRate ?? 100,
                WalletBalance = driver?.WalletBalance ?? 0
            };

            if (driver != null)
            {
                var allVehicles = await _vehicleRepo.GetAllAsync();
                var vehicle = allVehicles.FirstOrDefault(v => v.DriverId == driver.Id);

                if (vehicle != null)
                {
                    model.VehicleId = vehicle.Id;
                    model.CarBrand = vehicle.Brand;
                    model.CarModel = vehicle.Model;
                    model.LicensePlate = vehicle.LicensePlate;
                    model.CarYear = vehicle.Year;
                    model.CarColor = vehicle.Color;
                    model.PassengerSeats = vehicle.PassengerSeats;
                    model.InsuranceExpiryDate = vehicle.InsuranceExpiryDate;

                    var allVServices = await _vServiceRepo.GetAllAsync();
                    model.SelectedServiceIds = allVServices.Where(x => x.VehicleId == vehicle.Id).Select(x => x.AdditionalServiceId).ToList();

                    var allVClasses = await _vVehicleClassRepo.GetAllAsync();
                    model.SelectedVehicleClassIds = allVClasses.Where(x => x.VehicleId == vehicle.Id).Select(x => x.VehicleClassId).ToList();

                    var allPhotos = await _vPhotoRepo.GetAllAsync();
                    model.ExistingCarPhotos = allPhotos.Where(x => x.VehicleId == vehicle.Id).Select(x => x.PhotoPath).ToList();
                }
            }

            model.AvailableServices = (await _serviceRepo.GetAllAsync()).ToList();
            model.AvailableVehicleClasses = (await _vClassRepo.GetAllAsync()).ToList();

            SetLayoutData(driver ?? new TaxiLink.Domain.Models.Driver { User = currentUser });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(DriverProfileViewModel model)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Auth", new { area = "" });
            int userId = int.Parse(userIdClaim.Value);

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            user.FirstName = !string.IsNullOrWhiteSpace(model.FirstName) ? model.FirstName : user.FirstName;
            user.LastName = !string.IsNullOrWhiteSpace(model.LastName) ? model.LastName : user.LastName;
            user.PhoneNumber = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber : user.PhoneNumber;
            user.Email = !string.IsNullOrWhiteSpace(model.Email) ? model.Email : user.Email;

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

            var allDrivers = await _driverRepo.GetAllAsync();
            var driver = allDrivers.FirstOrDefault(d => d.UserId == userId);
            bool isNewDriver = false;

            if (driver == null)
            {
                driver = new TaxiLink.Domain.Models.Driver { UserId = userId, AcceptanceRate = 100, WalletBalance = 0 };
                isNewDriver = true;
            }

            driver.Patronymic = !string.IsNullOrWhiteSpace(model.Patronymic) ? model.Patronymic : driver.Patronymic;
            driver.DateOfBirth = model.DateOfBirth ?? driver.DateOfBirth;
            driver.TaxId = !string.IsNullOrWhiteSpace(model.TaxId) ? model.TaxId : driver.TaxId;
            driver.Iban = !string.IsNullOrWhiteSpace(model.Iban) ? model.Iban : driver.Iban;
            driver.IsFopActive = model.IsFopActive;

            if (isNewDriver)
            {
                await _driverRepo.AddAsync(driver);
                await _driverRepo.SaveChangesAsync();
            }
            else
            {
                _driverRepo.Update(driver);
                await _driverRepo.SaveChangesAsync();
            }

            var allVehicles = await _vehicleRepo.GetAllAsync();
            var vehicle = allVehicles.FirstOrDefault(v => v.DriverId == driver.Id);
            bool isNewVehicle = false;

            if (vehicle == null)
            {
                vehicle = new TaxiLink.Domain.Models.Vehicle { DriverId = driver.Id };
                isNewVehicle = true;
            }

            vehicle.Brand = !string.IsNullOrWhiteSpace(model.CarBrand) ? model.CarBrand : (vehicle.Brand ?? "Не вказано");
            vehicle.Model = !string.IsNullOrWhiteSpace(model.CarModel) ? model.CarModel : (vehicle.Model ?? "Не вказано");
            vehicle.Year = model.CarYear ?? (vehicle.Year == 0 ? DateTime.Now.Year : vehicle.Year);
            vehicle.Color = !string.IsNullOrWhiteSpace(model.CarColor) ? model.CarColor : (vehicle.Color ?? "Не вказано");
            vehicle.LicensePlate = !string.IsNullOrWhiteSpace(model.LicensePlate) ? model.LicensePlate : (vehicle.LicensePlate ?? "Не вказано");
            vehicle.PassengerSeats = model.PassengerSeats ?? (vehicle.PassengerSeats == 0 ? 4 : vehicle.PassengerSeats);
            vehicle.InsuranceExpiryDate = model.InsuranceExpiryDate ?? vehicle.InsuranceExpiryDate;

            if (isNewVehicle)
            {
                await _vehicleRepo.AddAsync(vehicle);
                await _vehicleRepo.SaveChangesAsync();
            }
            else
            {
                _vehicleRepo.Update(vehicle);
                await _vehicleRepo.SaveChangesAsync();
            }

            var oldClasses = (await _vVehicleClassRepo.GetAllAsync()).Where(x => x.VehicleId == vehicle.Id).ToList();
            foreach (var oc in oldClasses) _vVehicleClassRepo.Delete(oc);

            var oldServices = (await _vServiceRepo.GetAllAsync()).Where(x => x.VehicleId == vehicle.Id).ToList();
            foreach (var os in oldServices) _vServiceRepo.Delete(os);

            await _vVehicleClassRepo.SaveChangesAsync();

            if (model.SelectedVehicleClassIds != null && model.SelectedVehicleClassIds.Any())
            {
                foreach (var classId in model.SelectedVehicleClassIds)
                {
                    await _vVehicleClassRepo.AddAsync(new TaxiLink.Domain.Models.VehicleVehicleClass { VehicleId = vehicle.Id, VehicleClassId = classId });
                }
            }

            if (model.SelectedServiceIds != null && model.SelectedServiceIds.Any())
            {
                foreach (var srvId in model.SelectedServiceIds)
                {
                    await _vServiceRepo.AddAsync(new TaxiLink.Domain.Models.VehicleAdditionalService { VehicleId = vehicle.Id, AdditionalServiceId = srvId });
                }
            }

            if (model.CarPhotosUpload != null && model.CarPhotosUpload.Any())
            {
                string carPhotosFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "cars");
                if (!Directory.Exists(carPhotosFolder)) Directory.CreateDirectory(carPhotosFolder);

                foreach (var photoFile in model.CarPhotosUpload)
                {
                    if (photoFile.Length > 0)
                    {
                        string pFileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                        using (var fs = new FileStream(Path.Combine(carPhotosFolder, pFileName), FileMode.Create))
                        {
                            await photoFile.CopyToAsync(fs);
                        }
                        var vPhoto = new TaxiLink.Domain.Models.VehiclePhoto
                        {
                            VehicleId = vehicle.Id,
                            PhotoPath = "/img/cars/" + pFileName
                        };
                        await _vPhotoRepo.AddAsync(vPhoto);
                    }
                }
            }

            await _vehicleRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Profile));
        }

        public class StatusUpdateModel
        {
            public bool Status { get; set; }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleWorkingMode([FromBody] StatusUpdateModel model)
        {
            if (_currentDriver == null) return Json(new { success = false });

            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);
            if (driver != null)
            {
                driver.IsWorkingMode = model.Status;
                _driverRepo.Update(driver);
                await _driverRepo.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });
            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            SetLayoutData(details);

            var allOrders = await _orderRepo.GetAllAsync();
            var allOrderSrv = await _orderSrvRepo.GetAllAsync();
            var allServices = await _serviceRepo.GetAllAsync();
            var allUsers = await _userRepo.GetAllAsync();

            var driverOrders = allOrders
                .Where(o => o.DriverId == null || o.DriverId == _currentDriver.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => {
                    var passenger = allUsers.FirstOrDefault(u => u.Id == o.UserId);
                    string pName = o.PassengerName;
                    if (string.IsNullOrEmpty(pName))
                    {
                        pName = (passenger?.Id == _currentDriver.UserId) ? "Олена (Тестовий клієнт)" : passenger?.FirstName;
                    }

                    var srvIds = o.OrderAdditionalServices?.Select(x => x.AdditionalServiceId).ToList();
                    if (srvIds == null || !srvIds.Any())
                    {
                        srvIds = allOrderSrv.Where(x => x.OrderId == o.Id).Select(x => x.AdditionalServiceId).ToList();
                    }
                    var srvNames = allServices.Where(x => srvIds.Contains(x.Id)).Select(x => x.Name).ToList();

                    return new DriverOrderViewModel
                    {
                        Id = o.Id,
                        PickupAddress = o.PickupAddress,
                        DropoffAddress = o.DropoffAddress,
                        Distance = o.Distance,
                        TotalPrice = o.TotalPrice,
                        ScheduledTime = o.ScheduledTime,
                        CreatedAt = o.CreatedAt,
                        StatusName = o.OrderStatus?.Name ?? "Очікується",
                        PaymentMethodName = o.PaymentMethod?.Name ?? "Готівка",
                        VehicleClassName = o.VehicleClass?.Name ?? "Стандарт",
                        PassengerName = pName ?? "Клієнт",
                        SelectedServices = srvNames
                    };
                }).ToList();

            return View(driverOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Work()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });
            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            SetLayoutData(details);
            ViewBag.CancelReasons = await _cancelReasonRepo.GetAllAsync();

            var allOrders = await _orderRepo.GetAllAsync();
            var activeOrder = allOrders.FirstOrDefault(o => o.DriverId == _currentDriver.Id && o.OrderStatus?.Name != "Завершено" && o.OrderStatus?.Name != "Скасовано");

            if (activeOrder != null)
            {
                var allOrderSrv = await _orderSrvRepo.GetAllAsync();
                var allServices = await _serviceRepo.GetAllAsync();
                var allUsers = await _userRepo.GetAllAsync();

                var passenger = allUsers.FirstOrDefault(u => u.Id == activeOrder.UserId);
                string pName = activeOrder.PassengerName;
                if (string.IsNullOrEmpty(pName))
                {
                    pName = (passenger?.Id == _currentDriver.UserId) ? "Олена (Тестовий клієнт)" : passenger?.FirstName;
                }

                var srvIds = activeOrder.OrderAdditionalServices?.Select(x => x.AdditionalServiceId).ToList();
                if (srvIds == null || !srvIds.Any())
                {
                    srvIds = allOrderSrv.Where(x => x.OrderId == activeOrder.Id).Select(x => x.AdditionalServiceId).ToList();
                }
                var srvNames = allServices.Where(x => srvIds.Contains(x.Id)).Select(x => x.Name).ToList();

                var model = new DriverOrderViewModel
                {
                    Id = activeOrder.Id,
                    PickupAddress = activeOrder.PickupAddress,
                    DropoffAddress = activeOrder.DropoffAddress,
                    PassengerName = pName ?? "Клієнт",
                    ClientComment = activeOrder.ClientComment,
                    Distance = activeOrder.Distance,
                    TotalPrice = activeOrder.TotalPrice,
                    StatusName = activeOrder.OrderStatus?.Name ?? "В дорозі",
                    SelectedServices = srvNames
                };
                return View(model);
            }
            return View(new DriverOrderViewModel { Id = 0 });
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentWeather(string lat = "50.4501", string lon = "30.5234")
        {
            var weatherImpact = await _weatherService.GetWeatherImpactAsync(lat.Replace(",", "."), lon.Replace(",", "."));
            return Json(new { success = true, condition = weatherImpact.ConditionName, multiplier = weatherImpact.TimeMultiplier });
        }

        [HttpGet]
        public async Task<IActionResult> GetRouteData(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return Json(new { success = false });

            var startCoords = await _geocodingService.GetCoordinatesAsync(order.PickupAddress + ", Київ");
            var endCoords = await _geocodingService.GetCoordinatesAsync(order.DropoffAddress + ", Київ");

            if (startCoords == null) startCoords = ("50.4501", "30.5234");
            if (endCoords == null) endCoords = ("50.4422", "30.5367");

            var routeInfo = await _routingService.GetRouteInfoAsync(
                startCoords.Value.Lat, startCoords.Value.Lon,
                endCoords.Value.Lat, endCoords.Value.Lon
            );

            if (routeInfo == null)
                return Json(new { success = false, message = "Не вдалося побудувати маршрут." });

            string sLat = startCoords.Value.Lat.Replace(",", ".");
            string sLon = startCoords.Value.Lon.Replace(",", ".");
            var weatherImpact = await _weatherService.GetWeatherImpactAsync(sLat, sLon);
            double adjustedDuration = Math.Ceiling(routeInfo.Value.DurationMinutes * weatherImpact.TimeMultiplier);

            var usdRate = await _currencyService.GetRateAsync("USD");
            decimal priceUsd = usdRate.HasValue && usdRate.Value > 0
                ? Math.Round(order.TotalPrice / usdRate.Value, 2)
                : 0;

            return Json(new
            {
                success = true,
                distance = routeInfo.Value.DistanceKm,
                duration = adjustedDuration,
                priceUsd = priceUsd,
                routeCoordinates = routeInfo.Value.Coordinates,
                weatherCondition = weatherImpact.ConditionName,
                weatherMultiplier = weatherImpact.TimeMultiplier
            });
        }

        [HttpGet]
        public async Task<IActionResult> News()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            SetLayoutData(details);
            var allNews = await _newsRepo.GetAllAsync();
            var sortedNews = allNews.OrderByDescending(n => n.PublishedAt).ToList();
            return View(sortedNews);
        }

        [HttpGet]
        public async Task<IActionResult> Wallet()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var driver = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            if (driver == null) return NotFound();

            SetLayoutData(driver);

            var allOrders = await _orderRepo.GetAllAsync();
            var completedOrders = allOrders
                .Where(o => o.DriverId == driver.Id && o.OrderStatus?.Name == "Завершено")
                .ToList();

            var model = new WalletViewModel
            {
                WalletBalance = driver.WalletBalance
            };

            foreach (var order in completedOrders)
            {
                DateTime opDate = order.CompletedAt ?? order.CreatedAt;
                string paymentType = order.PaymentMethod?.Name?.ToLower() == "картка" ? "карткою" : "готівкою";

                model.Transactions.Add(new TransactionItemViewModel
                {
                    Title = $"Оплата {paymentType} (Замовлення #{order.Id})",
                    Date = opDate,
                    Amount = order.TotalPrice,
                    IsIncome = true
                });

                decimal commissionAmount = order.TotalPrice * (driver.CommissionRate / 100m);
                if (commissionAmount > 0)
                {
                    model.Transactions.Add(new TransactionItemViewModel
                    {
                        Title = $"Комісія сервісу ({driver.CommissionRate:0}%)",
                        Date = opDate,
                        Amount = commissionAmount,
                        IsIncome = false
                    });
                }
            }
            model.Transactions = model.Transactions.OrderByDescending(t => t.Date).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RefillWallet(decimal amount)
        {
            if (_currentDriver == null || amount <= 0)
                return Json(new { success = false, message = "Некоректна сума" });
            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);
            if (driver != null)
            {
                driver.WalletBalance += amount;
                _driverRepo.Update(driver);
                await _driverRepo.SaveChangesAsync();
                return Json(new { success = true, newBalance = driver.WalletBalance.ToString("N2") });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> WithdrawWallet()
        {
            if (_currentDriver == null) return Json(new { success = false });

            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);
            if (driver != null)
            {
                if (driver.WalletBalance <= 0)
                    return Json(new { success = false, message = "Немає коштів для виведення" });
                driver.WalletBalance = 0;
                _driverRepo.Update(driver);
                await _driverRepo.SaveChangesAsync();
                return Json(new { success = true, newBalance = "0,00" });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptOrder(int orderId)
        {
            if (_currentDriver == null) return Json(new { success = false });

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null && order.DriverId == null)
            {
                order.DriverId = _currentDriver.Id;
                var acceptedStatus = (await _statusRepo.GetAllAsync()).FirstOrDefault(s => s.Name == "В дорозі до клієнта");
                order.OrderStatusId = acceptedStatus?.Id ?? 2;

                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Замовлення вже зайняте" });
        }

        [HttpPost]
        public async Task<IActionResult> StartRide(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null && order.DriverId == _currentDriver.Id)
            {
                var activeStatus = (await _statusRepo.GetAllAsync()).FirstOrDefault(s => s.Name == "Виконується");
                order.OrderStatusId = activeStatus?.Id ?? order.OrderStatusId;

                _orderRepo.Update(order);
                await _orderRepo.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteRide(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);

            if (order != null && driver != null)
            {
                var completedStatus = (await _statusRepo.GetAllAsync()).FirstOrDefault(s => s.Name == "Завершено");
                order.OrderStatusId = completedStatus?.Id ?? 4;
                order.CompletedAt = DateTime.Now;

                decimal commission = order.TotalPrice * (driver.CommissionRate / 100m);
                if (order.PaymentMethodId == 1) { driver.WalletBalance -= commission; }
                else { driver.WalletBalance += (order.TotalPrice - commission); }

                _orderRepo.Update(order);
                _driverRepo.Update(driver);
                await _orderRepo.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId, int reasonId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);
            decimal penalty = 20.00m;

            if (order != null && driver != null)
            {
                var cancelStatus = (await _statusRepo.GetAllAsync()).FirstOrDefault(s => s.Name == "Скасовано");
                order.OrderStatusId = cancelStatus?.Id ?? 5;
                order.CancellationReasonId = reasonId;

                driver.WalletBalance -= penalty;

                _orderRepo.Update(order);
                _driverRepo.Update(driver);
                await _orderRepo.SaveChangesAsync();
                return Json(new { success = true, message = "Замовлення скасовано" });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> FinishOrder(int orderId, int rating)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            var passenger = await _userRepo.GetByIdAsync(order.UserId);

            if (order != null && passenger != null)
            {
                passenger.Rating = (passenger.Rating + rating) / 2;
                _userRepo.Update(passenger);
                return await CompleteRide(orderId);
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(string pickup, string dropoff, decimal distance, int vehicleClassId, string comment, List<int> selectedServices, decimal finalPrice)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

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

            if (selectedServices != null && selectedServices.Any())
            {
                foreach (var srvId in selectedServices)
                {
                    var orderSrv = new OrderAdditionalService
                    {
                        OrderId = order.Id,
                        AdditionalServiceId = srvId
                    };
                    await _orderSrvRepo.AddAsync(orderSrv);
                }
                await _orderSrvRepo.SaveChangesAsync();
            }

            return RedirectToAction("Orders", "Dashboard", new { area = "Driver" });
        }
    }
}