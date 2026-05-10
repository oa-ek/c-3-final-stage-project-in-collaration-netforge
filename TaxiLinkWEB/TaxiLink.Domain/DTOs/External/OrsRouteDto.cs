using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaxiLink.Domain.DTOs.External
{
    public class OrsRouteDto
    {
        [JsonPropertyName("features")]
        public List<OrsFeature> Features { get; set; }
    }

    public class OrsFeature
    {
        [JsonPropertyName("properties")]
        public OrsProperties Properties { get; set; }

        [JsonPropertyName("geometry")]
        public OrsGeometry Geometry { get; set; }
    }

    public class OrsProperties
    {
        [JsonPropertyName("summary")]
        public OrsSummary Summary { get; set; }
    }

    public class OrsSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class OrsGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<List<double>> Coordinates { get; set; }
    }
}
