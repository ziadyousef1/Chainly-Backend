namespace Chainly.Application.DTOs.Suppliers;

public class SupplierCreateDto
{
    public string Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? FacilityType { get; set; }
    public string? Sector { get; set; }
    public List<int>? MaterialIds { get; set; }
}