using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs
{
    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int? CompanyId { get; set; }
        public double Distance { get; set; }
        public string FacilityType { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
    }
}
