namespace Chainly.Application.DTOs.Materials;

public class MaterialDetailsDto
{
    public string Name { get; set; }
    public string CarbonFootprint { get; set; }
    public List<SupplierCarbonFootprintDto> Suppliers { get; set; } = new();
}

public class SupplierCarbonFootprintDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string CarbonFootprint { get; set; }
}