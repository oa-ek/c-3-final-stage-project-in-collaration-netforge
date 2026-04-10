using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using TaxiLink.Data.Repositories.Interfaces;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Models;

namespace TaxiLink.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGenericRepository<Driver> _driverRepo;
        private readonly IDataProtector _protector;

        public AuthController(
            IUserService userService,
            IGenericRepository<Driver> driverRepo,
            IDataProtectionProvider dataProtectionProvider)
        {
            _userService = userService;
            _driverRepo = driverRepo;
            _protector = dataProtectionProvider.CreateProtector("PasswordResetToken");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var users = await _userService.GetAllUsersAsync();
                var user = users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null)
                {
                    bool isPasswordValid = false;
                    bool needsUpgrade = false;

                    if (user.PasswordHash.StartsWith("$2"))
                    {
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                    }
                    else
                    {
                        isPasswordValid = (user.PasswordHash == model.Password);
                        needsUpgrade = true;
                    }

                    if (isPasswordValid)
                    {
                        if (needsUpgrade)
                        {
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                            await _userService.UpdateUserAsync(user);
                        }

                        await Authenticate(user, model.RememberMe);
                        return RedirectToRoleDashboard(user.RoleId);
                    }
                }
                ModelState.AddModelError("", "Невірний Email або пароль");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var users = await _userService.GetAllUsersAsync();
                if (users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email вже зайнятий");
                    return View(model);
                }

                var newUser = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    RoleId = model.RoleId,
                    RegistrationDate = DateTime.Now
                };

                await _userService.CreateUserAsync(newUser);

                if (newUser.RoleId == 2)
                {
                    var newDriver = new Driver
                    {
                        UserId = newUser.Id,
                        IsVerified = false,
                        IsWorkingMode = false,
                        CommissionRate = 10.0m,
                        WalletBalance = 0m
                    };
                    await _driverRepo.AddAsync(newDriver);
                    await _driverRepo.SaveChangesAsync();
                }

                await Authenticate(newUser, false);
                return RedirectToRoleDashboard(newUser.RoleId);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("ExternalLoginCallback") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("ExternalLoginCallback") };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    FirstName = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "User",
                    LastName = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "External",
                    Email = email,
                    PhoneNumber = "0000000000",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    RoleId = 3,
                    RegistrationDate = DateTime.Now
                };
                await _userService.CreateUserAsync(user);
            }

            await Authenticate(user, true);
            return RedirectToRoleDashboard(user.RoleId);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var users = await _userService.GetAllUsersAsync();
                var user = users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    string rawToken = $"{user.Id}:{DateTime.UtcNow.AddHours(1).Ticks}";
                    string encryptedToken = _protector.Protect(rawToken);
                    string resetLink = Url.Action("ResetPassword", "Auth", new { token = encryptedToken, email = user.Email }, Request.Scheme);

                    await SendEmailAsync(user.Email, "Відновлення пароля TaxiLink", $"Для скидання пароля перейдіть за посиланням: {resetLink}");
                }
                ViewBag.Message = "Якщо такий Email існує, на нього відправлено інструкції.";
                return View();
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string rawToken = _protector.Unprotect(model.Token);
                    var parts = rawToken.Split(':');
                    int userId = int.Parse(parts[0]);
                    long ticks = long.Parse(parts[1]);

                    if (DateTime.UtcNow.Ticks > ticks)
                    {
                        ModelState.AddModelError("", "Термін дії посилання вичерпано.");
                        return View(model);
                    }

                    var users = await _userService.GetAllUsersAsync();
                    var user = users.FirstOrDefault(u => u.Id == userId && u.Email == model.Email);

                    if (user != null)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                        await _userService.UpdateUserAsync(user);
                        return RedirectToAction("Login");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Недійсний токен відновлення.");
                }
            }
            return View(model);
        }

        private async Task Authenticate(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : (user.RoleId == 2 ? "Driver" : "Passenger"))
            };

            var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var props = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id), props);
        }

        private IActionResult RedirectToRoleDashboard(int roleId)
        {
            if (roleId == 1) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (roleId == 2) return RedirectToAction("Index", "Dashboard", new { area = "Driver" });
            return RedirectToAction("Index", "Home");
        }

        private async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password")
                };
                await client.SendMailAsync(new MailMessage("your-email@gmail.com", email, subject, message));
            }
            catch { }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}