using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class Blacklist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BlockerUserId { get; set; }
        [ForeignKey("BlockerUserId")]
        public User BlockerUser { get; set; }

        [Required]
        public int BlockedUserId { get; set; }
        [ForeignKey("BlockedUserId")]
        public User BlockedUser { get; set; }

        public DateTime BlockedAt { get; set; } = DateTime.Now;
    }
}
