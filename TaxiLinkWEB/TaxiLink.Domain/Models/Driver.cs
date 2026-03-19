using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiLink.Domain.Models
{
    public class Driver
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [MaxLength(100)]
        public string? Patronymic { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? TaxId { get; set; }

        public bool IsVerified { get; set; } = false;

        public decimal AcceptanceRate { get; set; } = 100.0m;

        public decimal WalletBalance { get; set; } = 0m;

        public bool IsFopActive { get; set; } = false;

        [MaxLength(50)]
        public string? Iban { get; set; }

        public decimal CommissionRate { get; set; } = 10.0m;

        [MaxLength(50)]
        public string? ReferralCode { get; set; }

        public bool IsWorkingMode { get; set; } = false;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
