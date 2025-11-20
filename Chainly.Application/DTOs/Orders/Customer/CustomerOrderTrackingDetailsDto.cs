namespace Chainly.Application.DTOs.Orders.Customer;

public class CustomerOrderTrackingDetailsDto
{
    public string Status { get; set; }
    public string Location { get; set; }
    
    public double Longitude { get; set; }
    
    public double Latitude { get; set; }
    public string Timestamp { get; set; }
}

public class CustomerOrderDto
{
    public string CompanyName { get; set; }
    public string Code { get; set; }
    public List<CustomerOrderTrackingDetailsDto> Trackings { get; set; }
}