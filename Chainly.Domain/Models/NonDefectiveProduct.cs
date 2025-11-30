namespace Chainly.Domain.Models;

public class NonDefectiveProduct
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    public string ProductId { get; set; }
}