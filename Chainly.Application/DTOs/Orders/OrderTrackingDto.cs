using Chainly.Domain.Constants;

namespace Chainly.Application.DTOs;

public class OrderTrackingDto
{
    public int OrderId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public OrderStatus Status { get; set; }
    public string Notes { get; set; }
}