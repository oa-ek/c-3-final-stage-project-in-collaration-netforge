using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class AdditionalService
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        public bool IsPercentage { get; set; } = false;

        public ICollection<VehicleAdditionalService> CapableVehicles { get; set; } = new List<VehicleAdditionalService>();
        public ICollection<OrderAdditionalService> OrderAdditionalServices { get; set; } = new List<OrderAdditionalService>();
    }
}
