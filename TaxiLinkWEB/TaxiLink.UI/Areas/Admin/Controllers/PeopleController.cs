using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.UI.Admin_areas.Models;

namespace TaxiLink.UI.Admin_areas.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PeopleController : Controller
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Driver> _driverRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Blacklist> _blacklistRepo;
        private readonly IWebHostEnvironment _env;

        public PeopleController(
            IGenericRepository<User> userRepo,
            IGenericRepository<Driver> driverRepo,
            IGenericRepository<Role> roleRepo,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Blacklist> blacklistRepo,
            IWebHostEnvironment env)
        {
            _userRepo = userRepo;
            _driverRepo = driverRepo;
            _roleRepo = roleRepo;
            _reviewRepo = reviewRepo;
            _blacklistRepo = blacklistRepo;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = await _userRepo.GetAllAsync();
            var allDrivers = await _driverRepo.GetAllAsync();

            var driverUserIds = allDrivers.Select(d => d.UserId).ToList();
            var clients = allUsers.Where(u => !driverUserIds.Contains(u.Id)).ToList();

            foreach (var d in allDrivers)
            {
                d.User = allUsers.FirstOrDefault(u => u.Id == d.UserId);
            }

            var reviews = await _reviewRepo.GetAllAsync();
            var blacklists = await _blacklistRepo.GetAllAsync();

            foreach (var b in blacklists)
            {
                b.BlockerUser = allUsers.FirstOrDefault(u => u.Id == b.BlockerUserId);
                b.BlockedUser = allUsers.FirstOrDefault(u => u.Id == b.BlockedUserId);
            }

            var model = new AdminViewModels.PeoplePageViewModel
            {
                Clients = clients,
                Drivers = allDrivers,
                Reviews = reviews,
                Blacklists = blacklists
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id) => Json(await _userRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertUser(User user, IFormFile? AvatarFile)
        {
            var roles = await _roleRepo.GetAllAsync();
            int clientRoleId = roles.FirstOrDefault(r => r.Name.ToLower().Contains("client") || r.Name.ToLower().Contains("клієнт"))?.Id ?? 2;

            user.AvatarPath = await ProcessAvatarAsync(AvatarFile, user.AvatarPath);

            if (user.Id == 0)
            {
                user.RegistrationDate = DateTime.Now;
                user.PasswordHash = "Client123!";
                user.RoleId = clientRoleId;
                await _userRepo.AddAsync(user);
            }
            else
            {
                var existingUser = await _userRepo.GetByIdAsync(user.Id);
                if (existingUser != null)
                {
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Email = user.Email;
                    existingUser.Rating = user.Rating;
                    existingUser.PrefersSilentRide = user.PrefersSilentRide;
                    existingUser.PrefersNoMusic = user.PrefersNoMusic;
                    if (!string.IsNullOrEmpty(user.AvatarPath)) existingUser.AvatarPath = user.AvatarPath;

                    _userRepo.Update(existingUser);
                }
            }
            await _userRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetDriverDetails(int id)
        {
            var driver = await _driverRepo.GetByIdAsync(id);
            if (driver == null) return NotFound();
            var user = await _userRepo.GetByIdAsync(driver.UserId);

            return Json(new AdminViewModels.DriverUpsertDto
            {
                DriverId = driver.Id,
                UserId = user?.Id ?? 0,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                PhoneNumber = user?.PhoneNumber,
                Email = user?.Email,
                Patronymic = driver.Patronymic,
                TaxId = driver.TaxId,
                Iban = driver.Iban,
                DateOfBirth = driver.DateOfBirth,
                CommissionRate = driver.CommissionRate,
                WalletBalance = driver.WalletBalance,
                IsVerified = driver.IsVerified,
                IsWorkingMode = driver.IsWorkingMode,
                AvatarPath = user?.AvatarPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpsertDriver(AdminViewModels.DriverUpsertDto dto, IFormFile? AvatarFile)
        {
            var roles = await _roleRepo.GetAllAsync();
            int driverRoleId = roles.FirstOrDefault(r => r.Name.ToLower().Contains("driver") || r.Name.ToLower().Contains("водій"))?.Id ?? 3;
            string? newAvatarPath = await ProcessAvatarAsync(AvatarFile, dto.AvatarPath);

            if (dto.DriverId == 0)
            {
                var newUser = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    RoleId = driverRoleId,
                    PasswordHash = "Driver123!",
                    AvatarPath = newAvatarPath
                };
                await _userRepo.AddAsync(newUser);
                await _userRepo.SaveChangesAsync(); 

                var newDriver = new Driver
                {
                    UserId = newUser.Id,
                    Patronymic = dto.Patronymic,
                    TaxId = dto.TaxId,
                    Iban = dto.Iban,
                    DateOfBirth = dto.DateOfBirth,
                    CommissionRate = dto.CommissionRate,
                    WalletBalance = dto.WalletBalance,
                    IsVerified = dto.IsVerified,
                    IsWorkingMode = dto.IsWorkingMode
                };
                await _driverRepo.AddAsync(newDriver);
            }
            else
            {
                var existingDriver = await _driverRepo.GetByIdAsync(dto.DriverId);
                if (existingDriver != null)
                {
                    var existingUser = await _userRepo.GetByIdAsync(existingDriver.UserId);
                    if (existingUser != null)
                    {
                        existingUser.FirstName = dto.FirstName;
                        existingUser.LastName = dto.LastName;
                        existingUser.PhoneNumber = dto.PhoneNumber;
                        existingUser.Email = dto.Email;
                        if (!string.IsNullOrEmpty(newAvatarPath)) existingUser.AvatarPath = newAvatarPath;

                        _userRepo.Update(existingUser);
                    }

                    existingDriver.Patronymic = dto.Patronymic;
                    existingDriver.TaxId = dto.TaxId;
                    existingDriver.Iban = dto.Iban;
                    existingDriver.DateOfBirth = dto.DateOfBirth;
                    existingDriver.CommissionRate = dto.CommissionRate;
                    existingDriver.WalletBalance = dto.WalletBalance;
                    existingDriver.IsVerified = dto.IsVerified;
                    existingDriver.IsWorkingMode = dto.IsWorkingMode;

                    _driverRepo.Update(existingDriver);
                }
            }
            await _driverRepo.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user != null) { _userRepo.Delete(user); await _userRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _driverRepo.GetByIdAsync(id);
            if (driver != null) { _driverRepo.Delete(driver); await _driverRepo.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDriver(int id, bool approve)
        {
            var driver = await _driverRepo.GetByIdAsync(id);
            if (driver != null)
            {
                driver.IsVerified = approve;
                _driverRepo.Update(driver);
                await _driverRepo.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _reviewRepo.GetByIdAsync(id);
            if (review != null)
            {
                _reviewRepo.Delete(review);
                await _reviewRepo.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromBlacklist(int id)
        {
            var blacklistRecord = await _blacklistRepo.GetByIdAsync(id);
            if (blacklistRecord != null)
            {
                _blacklistRepo.Delete(blacklistRecord);
                await _blacklistRepo.SaveChangesAsync();
            }
            return Ok();
        }

        private async Task<string?> ProcessAvatarAsync(IFormFile? file, string? currentPath)
        {
            if (file == null || file.Length == 0) return currentPath;
            string uploadsFolder = Path.Combine(_env.WebRootPath, "img");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                await file.CopyToAsync(fileStream);
            return "/img/" + uniqueFileName;
        }
    }
}