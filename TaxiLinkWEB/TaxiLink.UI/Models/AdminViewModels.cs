using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Domain.Models;

namespace TaxiLink.UI.Models
{
    public class AdminViewModels
    {
        public class OrderPageViewModel
        {
            public IEnumerable<Order> Orders { get; set; } = new List<Order>();
            public SelectList? Drivers { get; set; }
            public SelectList? Cities { get; set; }
            public SelectList? VehicleClasses { get; set; }
            public SelectList? PaymentMethods { get; set; }
            public SelectList Statuses { get; set; }
            public IEnumerable<AdditionalService> AvailableServices { get; set; } = new List<AdditionalService>();
        }
        public class PeoplePageViewModel
        {
            public IEnumerable<User> Clients { get; set; } = new List<User>();
            public IEnumerable<Driver> Drivers { get; set; } = new List<Driver>();
        }
        public class DriverUpsertDto
        {
            public int DriverId { get; set; }
            public int UserId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
            public string Patronymic { get; set; }
            public string TaxId { get; set; }
            public string Iban { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public decimal CommissionRate { get; set; }
            public decimal WalletBalance { get; set; }
            public bool IsVerified { get; set; }
            public string? AvatarPath { get; set; }
        }

        public class FleetPageViewModel
        {
            public IEnumerable<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
            public SelectList Drivers { get; set; }
            public IEnumerable<VehicleClass> AvailableClasses { get; set; } = new List<VehicleClass>();
            public IEnumerable<AdditionalService> AvailableServices { get; set; } = new List<AdditionalService>();
        }
        public class SettingsPageViewModel
        {
            public IEnumerable<City> Cities { get; set; } = new List<City>();
            public IEnumerable<AdditionalService> AdditionalServices { get; set; } = new List<AdditionalService>();
            public IEnumerable<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();
        }
        public class VehicleUpsertDto
        {
            public int Id { get; set; }
            public int DriverId { get; set; }
            public string Brand { get; set; }
            public string Model { get; set; }
            public int Year { get; set; }
            public string Color { get; set; }
            public string LicensePlate { get; set; }
            public int PassengerSeats { get; set; }
            public DateTime? InsuranceExpiryDate { get; set; }
            public List<int> SelectedClassIds { get; set; } = new List<int>();
            public List<int> SelectedServiceIds { get; set; } = new List<int>();
            public string? ExistingPhotoPath { get; set; }
        }
        public class MarketingPageViewModel
        {
            public IEnumerable<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();
            public IEnumerable<NewsItem> NewsItems { get; set; } = new List<NewsItem>();
        }
    }
}

