namespace TaxiLink.UI.Areas.Driver.Models
{
    public class DriverProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Patronymic { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? TaxId { get; set; } 
        public string? AvatarPath { get; set; }
        public bool IsVerified { get; set; }
        public decimal Rating { get; set; }
        public decimal AcceptanceRate { get; set; }
        public decimal WalletBalance { get; set; }
        public string? Iban { get; set; }
        public bool IsFopActive { get; set; }
        public string? CarBrand { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public int? CarYear { get; set; }
        public string? CarColor { get; set; }
    }
}
