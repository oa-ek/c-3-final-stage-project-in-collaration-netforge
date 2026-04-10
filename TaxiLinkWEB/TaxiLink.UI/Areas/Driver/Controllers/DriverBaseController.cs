using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TaxiLink.Services.Interfaces;

namespace TaxiLink.UI.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize(Roles = "Driver")]
    public class DriverBaseController : Controller
    {
        protected readonly IDriverService _driverService;
        protected TaxiLink.Domain.Models.Driver _currentDriver;

        public DriverBaseController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var allDrivers = await _driverService.GetAllDriversAsync();
                    _currentDriver = allDrivers.FirstOrDefault(d => d.UserId == userId);

                    if (_currentDriver != null)
                    {
                        ViewBag.DriverName = User.Identity.Name;
                        ViewBag.IsVerified = _currentDriver.IsVerified;
                    }
                }
            }
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
