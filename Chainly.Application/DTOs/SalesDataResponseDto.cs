using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs
{
    public class SalesDataResponseDto
    {
        [JsonPropertyName("city_id")]
        public long CityId { get; set; }

        [JsonPropertyName("store_id")]
        public long StoreId { get; set; }

        [JsonPropertyName("management_group_id")]
        public long ManagementGroupId { get; set; }

        [JsonPropertyName("first_category_id")]
        public long FirstCategoryId { get; set; }

        [JsonPropertyName("second_category_id")]
        public long SecondCategoryId { get; set; }

        [JsonPropertyName("third_category_id")]
        public long ThirdCategoryId { get; set; }

        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("dt")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("sale_amount")]
        public double SaleAmount { get; set; }

        [JsonPropertyName("hours_sale")]
        public string HoursSale { get; set; } = string.Empty;

        [JsonPropertyName("stock_hour6_22_cnt")]
        public int StockHour6To22Count { get; set; }

        [JsonPropertyName("hours_stock_status")]
        public string HoursStockStatus { get; set; } = string.Empty;

        [JsonPropertyName("discount")]
        public double Discount { get; set; }

        [JsonPropertyName("holiday_flag")]
        public int HolidayFlag { get; set; }

        [JsonPropertyName("activity_flag")]
        public int ActivityFlag { get; set; }

        [JsonPropertyName("precpt")]
        public double Precipitation { get; set; }

        [JsonPropertyName("avg_temperature")]
        public double AverageTemperature { get; set; }

        [JsonPropertyName("avg_humidity")]
        public double AverageHumidity { get; set; }

        [JsonPropertyName("avg_wind_level")]
        public double AverageWindLevel { get; set; }
    }
}
