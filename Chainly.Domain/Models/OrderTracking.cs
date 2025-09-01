using System.ComponentModel.DataAnnotations;

namespace Chainly.Domain.Models;

public class OrderTracking
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int CompanyId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UpdatedBy { get; set; }
    public User UpdatedByNavigation { get; set; }
}