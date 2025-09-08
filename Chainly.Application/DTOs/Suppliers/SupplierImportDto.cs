using System.ComponentModel.DataAnnotations;

namespace Chainly.Application.DTOs.Suppliers
{
    public class SupplierImportDto
    {
        /// <summary>
        /// The ID from Excel file - used to preserve original IDs
        /// </summary>
        public int? ExcelId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal? LocationLatitude { get; set; }

        public decimal? LocationLongitude { get; set; }

        public string? Sector { get; set; }

        public string? FacilityType { get; set; }


    }
}
