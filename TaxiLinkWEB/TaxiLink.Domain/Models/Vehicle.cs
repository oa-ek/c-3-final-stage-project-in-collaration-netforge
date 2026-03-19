using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiLink.Domain.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DriverId { get; set; }
        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }

        [Required]
        [MaxLength(50)]
        public string Brand { get; set; }

        [Required]
        [MaxLength(50)]
        public string Model { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(30)]
        public string Color { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; }

        [Required]
        public int PassengerSeats { get; set; }

        public DateTime? InsuranceExpiryDate { get; set; }

        public ICollection<VehiclePhoto> Photos { get; set; } = new List<VehiclePhoto>();
        public ICollection<VehicleVehicleClass> VehicleClasses { get; set; } = new List<VehicleVehicleClass>();
        public ICollection<VehicleAdditionalService> ProvidedServices { get; set; } = new List<VehicleAdditionalService>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
