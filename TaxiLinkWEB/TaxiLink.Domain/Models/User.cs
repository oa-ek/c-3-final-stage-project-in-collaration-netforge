using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [MaxLength(255)]
        public string? AvatarPath { get; set; }

        public decimal Rating { get; set; } = 5.0m;

        public decimal BonusBalance { get; set; } = 0m;

        public bool PrefersSilentRide { get; set; } = false;

        public bool PrefersNoMusic { get; set; } = false;

        public int? DefaultCityId { get; set; }
        [ForeignKey("DefaultCityId")]
        public City? DefaultCity { get; set; }

        [MaxLength(100)]
        public string? FacebookId { get; set; }

        [MaxLength(100)]
        public string? GoogleId { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<SavedAddress> SavedAddress { get; set; } = new List<SavedAddress>();
        public ICollection<UserPaymentCard> PaymentCards { get; set; } = new List<UserPaymentCard>();
        [InverseProperty("BlockerUser")]
        public ICollection<Blacklist> BlockedByMe { get; set; } = new List<Blacklist>();
        [InverseProperty("BlockedUser")]
        public ICollection<Blacklist> BlockedMe { get; set; } = new List<Blacklist>();
    }
}
