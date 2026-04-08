using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Areas.Driver.Models;

namespace TaxiLink.UI.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize(Roles = "Driver")]
    public class DashboardController : Controller
    {
        private readonly IDriverService _driverService;
        private readonly IUserService _userService;

        public DashboardController(IDriverService driverService, IUserService userService)
        {
            _driverService = driverService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var allDrivers = await _driverService.GetAllDriversAsync();
            var driver = allDrivers.FirstOrDefault(d => d.UserId == userId);

            if (driver == null) return NotFound();

            var driverDetails = await _driverService.GetDriverWithDetailsAsync(driver.Id);
            return View(driverDetails);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var allDrivers = await _driverService.GetAllDriversAsync();
            var driver = allDrivers.FirstOrDefault(d => d.UserId == userId);
            var details = await _driverService.GetDriverWithDetailsAsync(driver.Id);

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
                CarBrand = details.Vehicles.FirstOrDefault()?.Brand,
                CarModel = details.Vehicles.FirstOrDefault()?.Model,
                LicensePlate = details.Vehicles.FirstOrDefault()?.LicensePlate
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWorkingMode(bool status)
        {
            return Json(new { success = true });
        }
    }
}
