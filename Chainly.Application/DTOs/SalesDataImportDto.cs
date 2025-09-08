using System.ComponentModel.DataAnnotations;

namespace Chainly.Application.DTOs
{
    public class SalesDataImportDto
    {
        [Required]
        public long CityId { get; set; }
        
        [Required]
        public long StoreId { get; set; }
        
        [Required]
        public long ManagementGroupId { get; set; }
        
        [Required]
        public long FirstCategoryId { get; set; }
        
        [Required]
        public long SecondCategoryId { get; set; }
        
        [Required]
        public long ThirdCategoryId { get; set; }
        
        [Required]
        public long ProductId { get; set; }
        
        [Required]
        public string Dt { get; set; } = string.Empty;
        
        public double SaleAmount { get; set; }
        
        public string? HoursSale { get; set; } // JSON string for sequence
        
        public int StockHour6_22Cnt { get; set; }
        
        public string? HoursStockStatus { get; set; } // JSON string for sequence
        
        public double Discount { get; set; } = 1.0; // Default no discount
        
        public int HolidayFlag { get; set; }
        
        public int ActivityFlag { get; set; }
        
        public double Precpt { get; set; }
        
        public double AvgTemperature { get; set; }
        
        public double AvgHumidity { get; set; }
        
        public double AvgWindLevel { get; set; }
    }
    
    public class BulkSalesDataImportDto
    {
        public List<SalesDataImportDto> SalesData { get; set; } = new();
        public DateTime ImportedAt { get; set; }
        public int TotalRecords { get; set; }
        public int? CompanyId { get; set; }
    }
}
