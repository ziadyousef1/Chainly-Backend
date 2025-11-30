using Chainly.Domain.Constants;

namespace Chainly.Application.DTOs;

public class OrderTrackingDto
{
    public int OrderId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public OrderStatus Status { get; set; }
    public string Notes { get; set; }
}