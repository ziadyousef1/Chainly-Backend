using System.ComponentModel.DataAnnotations;
using Chainly.Domain.Constants;

namespace Chainly.Domain.Models;

public class OrderTracking
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int CompanyId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public OrderStatus Status { get; set; }
    public string Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UpdatedBy { get; set; }
    public User UpdatedByNavigation { get; set; }
}