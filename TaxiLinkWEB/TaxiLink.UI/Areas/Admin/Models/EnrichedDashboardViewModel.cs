using TaxiLink.Domain.Models;
namespace TaxiLink.UI.Areas.Admin.Models
{
    public class EnrichedDashboardViewModel
    {
        public decimal TotalRevenueUah { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveDrivers { get; set; }

        public decimal? UsdExchangeRate { get; set; }
        public decimal? TotalRevenueUsd { get; set; }
    }
}
