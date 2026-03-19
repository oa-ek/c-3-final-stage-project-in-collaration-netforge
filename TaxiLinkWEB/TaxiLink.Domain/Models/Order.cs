using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public int? DriverId { get; set; }
        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }

        public int? VehicleId { get; set; }
        [ForeignKey("VehicleId")]
        public Vehicle Vehicle { get; set; }

        [Required]
        public int VehicleClassId { get; set; }
        [ForeignKey("VehicleClassId")]
        public VehicleClass VehicleClass { get; set; }

        [Required]
        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public City City { get; set; }

        [Required]
        public int OrderStatusId { get; set; }
        [ForeignKey("OrderStatusId")]
        public OrderStatus OrderStatus { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }
        [ForeignKey("PaymentMethodId")]
        public PaymentMethod PaymentMethod { get; set; }

        public int? PromoCodeId { get; set; }
        [ForeignKey("PromoCodeId")]
        public PromoCode PromoCode { get; set; }

        public int? CancellationReasonId { get; set; }
        [ForeignKey("CancellationReasonId")]
        public CancellationReason CancellationReason { get; set; }

        [Required]
        public string PickupAddress { get; set; }

        [Required]
        public string DropoffAddress { get; set; }

        public DateTime? ScheduledTime { get; set; }

        [MaxLength(100)]
        public string? PassengerName { get; set; }

        [MaxLength(20)]
        public string? PassengerPhone { get; set; }

        [MaxLength(500)]
        public string? ClientComment { get; set; }

        [Required]
        public decimal Distance { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        public decimal ClientPriceBonus { get; set; } = 0m;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        public Review Review { get; set; }
        public ICollection<OrderAdditionalService> OrderAdditionalServices { get; set; } = new List<OrderAdditionalService>();
    }
}
