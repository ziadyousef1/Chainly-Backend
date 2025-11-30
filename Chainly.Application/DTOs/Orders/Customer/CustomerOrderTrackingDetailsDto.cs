namespace Chainly.Application.DTOs.Orders.Customer;

public class CustomerOrderTrackingDetailsDto
{
    public string Status { get; set; }
    public string Location { get; set; }
    
    public decimal Longitude { get; set; }
    
    public decimal Latitude { get; set; }
    public string Timestamp { get; set; }
}

public class CustomerOrderDto
{
    public string CompanyName { get; set; }
    public string Code { get; set; }
    public List<CustomerOrderTrackingDetailsDto> Trackings { get; set; }
}