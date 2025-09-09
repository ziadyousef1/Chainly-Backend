namespace Chainly.Application.DTOs.ProductionLines;

public class ProductionLineCreateDto
{
    public string LineName { get; set; } = string.Empty;
    public string? Description { get; set; }
}