using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class VehicleAdditionalService
    {
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        public int AdditionalServiceId { get; set; }
        public AdditionalService AdditionalService { get; set; }
    }
}
