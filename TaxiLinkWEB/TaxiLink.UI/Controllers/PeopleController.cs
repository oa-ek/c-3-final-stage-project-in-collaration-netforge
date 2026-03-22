using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.UI.Models;

namespace TaxiLink.UI.Controllers
{
    public class PeopleController : Controller
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Driver> _driverRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IWebHostEnvironment _env;

        public PeopleController(
            IGenericRepository<User> userRepo,
            IGenericRepository<Driver> driverRepo,
            IGenericRepository<Role> roleRepo,
            IWebHostEnvironment env)
        {
            _userRepo = userRepo;
            _driverRepo = driverRepo;
            _roleRepo = roleRepo;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = await _userRepo.GetAllAsync();
            var allDrivers = await _driverRepo.GetAllAsync();
            var roles = await _roleRepo.GetAllAsync();
            int clientRoleId = roles.FirstOrDefault(r => r.Name.ToLower().Contains("client") || r.Name.ToLower().Contains("user"))?.Id ?? 3;
            var clients = allUsers.Where(u => u.RoleId == clientRoleId).ToList();
            foreach (var d in allDrivers)
            {
                d.User = allUsers.FirstOrDefault(u => u.Id == d.UserId);
            }

            var model = new AdminViewModels.PeoplePageViewModel
            {
                Clients = clients,
                Drivers = allDrivers
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id) => Json(await _userRepo.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpsertUser(User user, IFormFile? AvatarFile)
        {
            var roles = await _roleRepo.GetAllAsync();
            int clientRoleId = roles.FirstOrDefault(r => r.Name.ToLower().Contains("client") || r.Name.ToLower().Contains("user"))?.Id ?? 3;

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
                AvatarPath = user?.AvatarPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpsertDriver(AdminViewModels.DriverUpsertDto dto, IFormFile? AvatarFile)
        {
            var roles = await _roleRepo.GetAllAsync();
            int driverRoleId = roles.FirstOrDefault(r => r.Name.ToLower().Contains("driver"))?.Id ?? 2;
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
                    IsVerified = dto.IsVerified
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