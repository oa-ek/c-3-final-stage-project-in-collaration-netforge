using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class PromoCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }

        [Required]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public int MaxUses { get; set; }

        public int CurrentUses { get; set; } = 0;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
