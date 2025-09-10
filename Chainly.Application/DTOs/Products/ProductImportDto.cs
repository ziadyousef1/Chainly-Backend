namespace Chainly.Application.DTOs.Products
{
    public class ProductImportDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } 
        public int CompanyId { get; set; }
    }
}
