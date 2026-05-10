using TaxiLink.Domain.Models;

namespace TaxiLink.UI.Areas.Client.Models
{
    public class ClientDashboardViewModel
    {
        public User User { get; set; }
        public IEnumerable<VehicleClass> VehicleClasses { get; set; }
        public IEnumerable<AdditionalService> AdditionalServices { get; set; }
        public decimal CityMultiplier { get; set; }
        public decimal UsdRate { get; set; }
        public bool IsGoogleLinked => !string.IsNullOrEmpty(User?.GoogleId);
    }

    public class UserProfileEditModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool PrefersSilentRide { get; set; }
        public bool PrefersNoMusic { get; set; }
        public IFormFile AvatarUpload { get; set; }
    }
}
