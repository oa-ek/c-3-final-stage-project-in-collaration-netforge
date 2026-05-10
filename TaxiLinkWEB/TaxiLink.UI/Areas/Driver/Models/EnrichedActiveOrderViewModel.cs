using TaxiLink.Domain.Models;
namespace TaxiLink.UI.Areas.Driver.Models
{
    public class EnrichedActiveOrderViewModel
    {
        public Order CurrentOrder { get; set; }

        public double? DistanceKm { get; set; }
        public double? DurationMinutes { get; set; }

        public decimal? PriceInUsd { get; set; }
    }
}
