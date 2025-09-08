using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs.Suppliers
{
    public class SupplierCarbonFootprintDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalCarbon { get; set; }
        public int Rank { get; set; }
    }
}
