using System.ComponentModel.DataAnnotations;

namespace Chainly.Domain.Models
{
    public class SalesData
    {
        [Key]
        public int Id { get; set; }
        
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
        
        public string HoursSale { get; set; } = string.Empty; 
        
        public int StockHour6_22Cnt { get; set; }
        
        public string HoursStockStatus { get; set; } = string.Empty; 
        
        public double Discount { get; set; }
        
        public int HolidayFlag { get; set; }
        
        public int ActivityFlag { get; set; }
        
        public double Precpt { get; set; }
        
        public double AvgTemperature { get; set; }
        
        public double AvgHumidity { get; set; }
        
        public double AvgWindLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
