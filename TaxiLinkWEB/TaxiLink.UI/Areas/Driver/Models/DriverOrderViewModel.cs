namespace TaxiLink.UI.Areas.Driver.Models
{
    public class DriverOrderViewModel
    {
        public int Id { get; set; }
        public string PickupAddress { get; set; }
        public string DropoffAddress { get; set; }
        public decimal Distance { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StatusName { get; set; }
        public string PaymentMethodName { get; set; }
        public string VehicleClassName { get; set; }
        public string PassengerName { get; set; }
        public decimal PassengerRating { get; set; }
        public string ClientComment { get; set; }
        public List<string> SelectedServices { get; set; } = new List<string>();
    }
}
