using System.ComponentModel.DataAnnotations;

namespace Chainly.Domain.Models;

public class Order
{
    public int OrderId { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Code { get; set; }
    public ICollection<OrderTracking> Trackings { get; set; }
}