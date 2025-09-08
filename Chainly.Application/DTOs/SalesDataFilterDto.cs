namespace Chainly.Application.DTOs
{
    public class SalesDataFilterDto
    {
        public long? CityId { get; set; }
        public long? StoreId { get; set; }
        public long? ManagementGroupId { get; set; }
        public long? FirstCategoryId { get; set; }
        public long? SecondCategoryId { get; set; }
        public long? ThirdCategoryId { get; set; }
        public long? ProductId { get; set; }
        public int? Limit { get; set; } = 30;
    }
}
