using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs.Suppliers
{
    public class MaterialImportDto
    {
        /// <summary>
        /// The ID from Excel file - used to preserve original IDs
        /// </summary>
        public int? ExcelId { get; set; }

        public string Name { get; set; } = string.Empty;
        public decimal EmissionValue { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
