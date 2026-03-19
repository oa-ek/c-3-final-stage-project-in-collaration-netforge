using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class VehicleClass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public decimal BasePrice { get; set; }

        [Required]
        public decimal PricePerKm { get; set; }

        [Required]
        public decimal PricePerKmOutsideCity { get; set; }

        [Required]
        public decimal PricePerMinuteWaiting { get; set; }

        [Required]
        public decimal CancellationFee { get; set; }

        public ICollection<VehicleVehicleClass> Vehicles { get; set; } = new List<VehicleVehicleClass>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
