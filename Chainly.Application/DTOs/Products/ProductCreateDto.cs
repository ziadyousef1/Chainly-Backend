namespace Chainly.Application.DTOs.Products;

public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
}