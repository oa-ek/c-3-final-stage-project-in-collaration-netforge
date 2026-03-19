using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class UserPaymentCard

    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        [Required]
        [MaxLength(20)]
        public string CardMask { get; set; }
        [MaxLength(50)]
        public string PaymentSystem { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
