using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs.Suppliers
{
    public class MaterialImportDto
    {
        public string Name { get; set; }
        public decimal EmissionValue { get; set; }
        public string Unit { get; set; }
    }
}
