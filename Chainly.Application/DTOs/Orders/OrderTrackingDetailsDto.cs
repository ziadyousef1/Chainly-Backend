using System.Text.Json.Serialization;
using Chainly.Domain.Constants;

namespace Chainly.Application.DTOs.Orders;

public class OrderTrackingDetailsDto
{
    public int Id { get; set; }
    public string Status { get; set; }
    public string Notes { get; set; }
    public string Location { get; set; }

    public decimal Longitude { get; set; }

    public decimal Latitude { get; set; }
    public string Timestamp { get; set; }
    public UpdatedByDto UpdatedBy { get; set; }
}

public class UpdatedByDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int CompanyOrderId { get; set; }
    public string Code { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<OrderTrackingDetailsDto> Trackings { get; set; }
}