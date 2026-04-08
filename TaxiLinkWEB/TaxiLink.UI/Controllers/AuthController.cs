using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaxiLink.Domain.Models;
using TaxiLink.Services.Interfaces;
using TaxiLink.UI.Models;

namespace TaxiLink.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var users = await _userService.GetAllUsersAsync();
                var user = users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    await Authenticate(user, model.RememberMe);
                    return RedirectToRoleDashboard(user.RoleId);
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
                    PasswordHash = model.Password,
                    RoleId = model.RoleId
                };

                await _userService.CreateUserAsync(newUser);
                await Authenticate(newUser, false);
                return RedirectToRoleDashboard(newUser.RoleId);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleLoginCallback") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleLoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    FirstName = result.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "User",
                    LastName = result.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Google",
                    Email = email,
                    PhoneNumber = "0000000000",
                    PasswordHash = Guid.NewGuid().ToString(),
                    RoleId = 3
                };
                await _userService.CreateUserAsync(user);
            }

            await Authenticate(user, true);
            return RedirectToRoleDashboard(user.RoleId);
        }

        private async Task Authenticate(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}