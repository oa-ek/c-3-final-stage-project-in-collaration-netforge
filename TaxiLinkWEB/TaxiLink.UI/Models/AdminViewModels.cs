using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiLink.Domain.Models;

namespace TaxiLink.UI.Models
{
    public class AdminViewModels
    {
        // Коробка для сторінки замовлень
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

        // Коробка для сторінки людей (будемо робити далі)
        public class PeoplePageViewModel
        {
            public IEnumerable<User> Clients { get; set; } = new List<User>();
            public IEnumerable<Driver> Drivers { get; set; } = new List<Driver>();
        }

        // Коробка для автопарку (будемо робити далі)
        public class FleetPageViewModel
        {
            public IEnumerable<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
            public SelectList? Drivers { get; set; }
            public SelectList? VehicleClasses { get; set; }
        }
    }
}

