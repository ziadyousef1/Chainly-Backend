namespace Chainly.Application.DTOs.Orders;

public class OrderImportDto
{
    public int OrderId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
