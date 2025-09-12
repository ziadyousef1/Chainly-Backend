using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs.Suppliers
{
    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string FacilityType { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
    }
}