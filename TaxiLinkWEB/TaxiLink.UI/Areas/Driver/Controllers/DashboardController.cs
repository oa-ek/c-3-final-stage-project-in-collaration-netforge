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
            IOrderRepository orderRepo) : base(driverService)
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
        }

        private void SetLayoutData(TaxiLink.Domain.Models.Driver d)
        {
            ViewBag.IsWorkingMode = d.IsWorkingMode;
            ViewBag.WalletBalance = d.WalletBalance;
            ViewBag.Rating = d.User?.Rating ?? 5.0m;
            ViewBag.IsVerified = d.IsVerified;
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
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            if (details == null || details.User == null) return RedirectToAction("Login", "Auth", new { area = "" });

            SetLayoutData(details);
            var vehicle = details.Vehicles?.FirstOrDefault();

            var model = new DriverProfileViewModel
            {
                FirstName = details.User.FirstName,
                LastName = details.User.LastName,
                Patronymic = details.Patronymic,
                Email = details.User.Email,
                PhoneNumber = details.User.PhoneNumber,
                DateOfBirth = details.DateOfBirth,
                TaxId = details.TaxId,
                AvatarPath = details.User.AvatarPath,
                IsVerified = details.IsVerified,
                Rating = details.User.Rating,
                AcceptanceRate = details.AcceptanceRate,
                WalletBalance = details.WalletBalance,
                Iban = details.Iban,
                IsFopActive = details.IsFopActive,
                VehicleId = vehicle?.Id,
                CarBrand = vehicle?.Brand,
                CarModel = vehicle?.Model,
                LicensePlate = vehicle?.LicensePlate,
                CarYear = vehicle?.Year,
                CarColor = vehicle?.Color,
                PassengerSeats = vehicle?.PassengerSeats,
                InsuranceExpiryDate = vehicle?.InsuranceExpiryDate,
                AvailableServices = (await _serviceRepo.GetAllAsync()).ToList(),
                SelectedServiceIds = vehicle != null ? (await _vServiceRepo.GetAllAsync()).Where(x => x.VehicleId == vehicle.Id).Select(x => x.AdditionalServiceId).ToList() : new List<int>(),
                AvailableVehicleClasses = (await _vClassRepo.GetAllAsync()).ToList(),
                SelectedVehicleClassIds = vehicle != null ? (await _vVehicleClassRepo.GetAllAsync()).Where(x => x.VehicleId == vehicle.Id).Select(x => x.VehicleClassId).ToList() : new List<int>(),
                ExistingCarPhotos = vehicle != null ? (await _vPhotoRepo.GetAllAsync()).Where(x => x.VehicleId == vehicle.Id).Select(x => x.PhotoPath).ToList() : new List<string>()
            };
            return View(model);
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Profile(DriverProfileViewModel model)
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });

            var driver = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            if (driver == null || driver.User == null) return NotFound();
            if (model.AvatarUpload != null && model.AvatarUpload.Length > 0)
            {
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "avatars");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarUpload.FileName);
                using (var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.AvatarUpload.CopyToAsync(fs);
                }
                driver.User.AvatarPath = "/img/avatars/" + fileName;
            }
            if (!string.IsNullOrWhiteSpace(model.FirstName)) driver.User.FirstName = model.FirstName;
            if (!string.IsNullOrWhiteSpace(model.LastName)) driver.User.LastName = model.LastName;
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber)) driver.User.PhoneNumber = model.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(model.Email)) driver.User.Email = model.Email;

            driver.Patronymic = model.Patronymic;
            driver.DateOfBirth = model.DateOfBirth;
            driver.TaxId = model.TaxId;
            driver.Iban = model.Iban;
            driver.IsFopActive = model.IsFopActive;

            _userRepo.Update(driver.User);

            var v = driver.Vehicles.FirstOrDefault();
            if (v == null)
            {
                v = new Vehicle
                {
                    DriverId = driver.Id,
                    Brand = model.CarBrand ?? "Не вказано",
                    Model = model.CarModel ?? "Не вказано",
                    Year = model.CarYear ?? 2024,
                    Color = model.CarColor ?? "",
                    LicensePlate = model.LicensePlate ?? "",
                    PassengerSeats = model.PassengerSeats ?? 4
                };
                await _vehicleRepo.AddAsync(v);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(model.CarBrand)) v.Brand = model.CarBrand;
                if (!string.IsNullOrWhiteSpace(model.CarModel)) v.Model = model.CarModel;
                v.Year = model.CarYear ?? v.Year;
                v.Color = model.CarColor;
                v.LicensePlate = model.LicensePlate;
                v.PassengerSeats = model.PassengerSeats ?? v.PassengerSeats;
                v.InsuranceExpiryDate = model.InsuranceExpiryDate;
                _vehicleRepo.Update(v);
            }

            try
            {
                await _driverRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ПОМИЛКА ЗБЕРЕЖЕННЯ: " + ex.Message);
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWorkingMode(bool status)
        {
            if (_currentDriver == null) return Json(new { success = false });
            var driver = await _driverRepo.GetByIdAsync(_currentDriver.Id);
            if (driver != null)
            {
                driver.IsWorkingMode = status;
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
            var driverOrders = allOrders
                .Where(o => o.DriverId == null || o.DriverId == _currentDriver.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new DriverOrderViewModel
                {
                    Id = o.Id,
                    PickupAddress = o.PickupAddress,
                    DropoffAddress = o.DropoffAddress,
                    Distance = o.Distance,
                    TotalPrice = o.TotalPrice,
                    ScheduledTime = o.ScheduledTime,
                    CreatedAt = o.CreatedAt,
                    StatusName = o.OrderStatus?.Name ?? "Невідомо",
                    PaymentMethodName = o.PaymentMethod?.Name ?? "Невідомо",
                    VehicleClassName = o.VehicleClass?.Name ?? "Стандарт",
                    PassengerName = o.PassengerName ?? o.User?.FirstName ?? "Клієнт",
                    PassengerRating = o.User?.Rating ?? 5.0m
                }).ToList();

            return View(driverOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Work()
        {
            if (_currentDriver == null) return RedirectToAction("Login", "Auth", new { area = "" });
            var details = await _driverRepo.GetDriverWithDetailsAsync(_currentDriver.Id);
            SetLayoutData(details);

            var allOrders = await _orderRepo.GetAllAsync();
            var activeOrder = allOrders.FirstOrDefault(o => o.DriverId == _currentDriver.Id && o.OrderStatus?.Name != "Завершено" && o.OrderStatus?.Name != "Скасовано");

            DriverOrderViewModel model;
            if (activeOrder != null)
            {
                model = new DriverOrderViewModel
                {
                    Id = activeOrder.Id,
                    PickupAddress = activeOrder.PickupAddress,
                    DropoffAddress = activeOrder.DropoffAddress,
                    PassengerName = activeOrder.PassengerName ?? activeOrder.User?.FirstName ?? "Клієнт",
                    ClientComment = activeOrder.ClientComment,
                    Distance = activeOrder.Distance,
                    TotalPrice = activeOrder.TotalPrice,
                    StatusName = activeOrder.OrderStatus?.Name ?? "В дорозі до клієнта"
                };
            }
            else
            {
                model = new DriverOrderViewModel
                {
                    Id = 0,
                    PickupAddress = "вул. Хрещатик, 22",
                    DropoffAddress = "вул. Саксаганського, 45",
                    PassengerName = "Олена Коваленко",
                    ClientComment = "\"Буду з собакою\"",
                    Distance = 5.2m,
                    TotalPrice = 250,
                    StatusName = "В дорозі до клієнта"
                };
            }
            return View(model);
        }
    }
}