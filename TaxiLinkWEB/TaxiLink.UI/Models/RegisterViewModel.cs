using System.ComponentModel.DataAnnotations;

namespace TaxiLink.UI.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        public string FirstName { get; set; }

        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Номер телефону є обов'язковим")]
        [Phone(ErrorMessage = "Некоректний номер телефону")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [StringLength(100, ErrorMessage = "Пароль має бути не менше {2} символів", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не збігаються")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int RoleId { get; set; } 
    }
}
