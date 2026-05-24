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
using Microsoft.Extensions.Configuration;


namespace TaxiLink.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGenericRepository<Driver> _driverRepo;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUserService userService,
            IGenericRepository<Driver> driverRepo,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration configuration)
        {
            _userService = userService;
            _driverRepo = driverRepo;
            _protector = dataProtectionProvider.CreateProtector("PasswordResetToken");
            _configuration = configuration;
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

                        return await ProcessUserLogin(user, model.RememberMe);
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

                return await ProcessUserLogin(newUser, false);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("ExternalLoginCallback") };
            properties.Items["prompt"] = "select_account";
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
            var user = users.FirstOrDefault(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            var pictureUrl = result.Principal.FindFirst("urn:google:picture")?.Value ?? result.Principal.FindFirst("image")?.Value;

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
                    GoogleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier),
                    AvatarPath = pictureUrl,
                    RegistrationDate = DateTime.Now
                };
                await _userService.CreateUserAsync(user);
            }
            else
            {
                bool needsUpdate = false;
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    needsUpdate = true;
                }
                if (string.IsNullOrEmpty(user.AvatarPath) && !string.IsNullOrEmpty(pictureUrl))
                {
                    user.AvatarPath = pictureUrl;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await _userService.UpdateUserAsync(user);
                }
            }

            return await ProcessUserLogin(user, true);
        }
        private async Task<IActionResult> ProcessUserLogin(User user, bool isPersistent)
        {
            await AuthenticateLocal(user, isPersistent, user.RoleId);
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
                var user = users.FirstOrDefault(u => u.Email != null && u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    string rawToken = $"{user.Id}:{DateTime.UtcNow.AddHours(1).Ticks}";
                    string encryptedToken = _protector.Protect(rawToken);
                    string resetLink = Url.Action("ResetPassword", "Auth", new { token = encryptedToken, email = user.Email }, Request.Scheme);
                    string emailBody = $@"
                        <div style='font-family: ""Segoe UI"", Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                            
                            <div style='background-color: #1e293b; padding: 25px; text-align: center;'>
                                <h1 style='color: #facc15; margin: 0; font-size: 28px; letter-spacing: 1px;'>TaxiLink</h1>
                            </div>

                            <div style='padding: 30px; color: #334155;'>
                                <h2 style='margin-top: 0; font-size: 22px; color: #0f172a;'>Відновлення доступу</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>Шановний(а) <strong>{user.FirstName}</strong>,</p>
                                <p style='font-size: 16px; line-height: 1.6;'>Ми отримали запит на скидання пароля для вашого облікового запису в системі <strong>TaxiLink</strong>. Щоб створити новий пароль, будь ласка, натисніть на кнопку нижче:</p>
                                
                                <div style='text-align: center; margin: 35px 0;'>
                                    <a href='{resetLink}' style='background-color: #facc15; color: #0f172a; padding: 14px 30px; text-decoration: none; font-weight: 700; border-radius: 8px; font-size: 16px; display: inline-block;'>Скинути пароль</a>
                                </div>

                                <p style='font-size: 14px; color: #64748b; line-height: 1.5; margin-bottom: 5px;'>Якщо кнопка не працює, скопіюйте це посилання та вставте його в адресний рядок браузера:</p>
                                <p style='font-size: 13px; color: #3b82f6; word-break: break-all; margin-top: 0;'>{resetLink}</p>
                            </div>

                            <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;'>
                                <p style='margin: 0; font-size: 13px; color: #94a3b8;'>Якщо ви не надсилали цей запит, просто проігноруйте цей лист. Ваш пароль залишиться в безпеці.</p>
                                <p style='margin: 10px 0 0 0; font-size: 13px; color: #64748b; font-weight: 600;'>З повагою, команда TaxiLink</p>
                            </div>

                        </div>";

                    await SendEmailAsync(user.Email, "Відновлення пароля – TaxiLink", emailBody);
                }

                ViewBag.Message = "Якщо такий Email існує в нашій системі, на нього відправлено інструкції.";
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

        private async Task AuthenticateLocal(User user, bool isPersistent, int roleId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleId == 1 ? "Admin" : (roleId == 2 ? "Driver" : "Client"))
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
            return RedirectToAction("Index", "Dashboard", new { area = "Client" });
        }

        private async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                string senderEmail = _configuration["Smtp:Email"];
                string senderPassword = _configuration["Smtp:Password"];

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(senderEmail, senderPassword)
                };

                var mailMessage = new MailMessage(senderEmail, email, subject, message)
                {
                    IsBodyHtml = true 
                };

                await client.SendMailAsync(mailMessage);
            }
            catch { }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}