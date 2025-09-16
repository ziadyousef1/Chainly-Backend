using System.Text.Json.Serialization;
using Chainly.Domain.Constants;

namespace Chainly.Application.DTOs.Orders;

public class OrderTrackingDetailsDto
{
    public OrderStatus Status { get; set; }
    public string Notes { get; set; }
    public string Location { get; set; }
    [JsonIgnore]
    public double Longitude { get; set; }
    [JsonIgnore]
    public double Latitude { get; set; }
    public DateTime Timestamp { get; set; }
    public UpdatedByDto UpdatedBy { get; set; }
}

public class UpdatedByDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
}

public class OrderDto
{
    public string Code { get; set; }
    public List<OrderTrackingDetailsDto> Trackings { get; set; }
}