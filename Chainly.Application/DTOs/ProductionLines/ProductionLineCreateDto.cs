namespace Chainly.Application.DTOs.ProductionLines;

public class ProductionLineCreateDto
{
    public string LineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double MaximumSpeed { get; set; }
    public double RatedPower { get; set; }
    public double MaximumTemperature { get; set; }
    public double MaximumCurrent { get; set; }
}