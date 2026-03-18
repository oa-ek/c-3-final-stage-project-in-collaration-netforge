using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class OrderAdditionalService
    {
        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int AdditionalServiceId { get; set; }
        public AdditionalService AdditionalService { get; set; }
    }
}
