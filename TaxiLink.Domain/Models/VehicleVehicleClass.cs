using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiLink.Domain.Models
{
    public class VehicleVehicleClass
    {
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        public int VehicleClassId { get; set; }
        public VehicleClass VehicleClass { get; set; }
    }
}
