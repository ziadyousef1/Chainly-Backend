using System.ComponentModel.DataAnnotations;

namespace Chainly.Application.DTOs
{
    public class SupplierImportDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public decimal? LocationLatitude { get; set; }
        
        public decimal? LocationLongitude { get; set; }
        
        public string? Sector { get; set; }
        
        public string? FacilityType { get; set; }
        
        public string? ProductType { get; set; }
    }
}
