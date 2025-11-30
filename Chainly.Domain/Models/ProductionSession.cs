namespace Chainly.Domain.Models;

public class ProductionSession
{
    public int Id { get; set; }
    
    public string ProductId { get; set; }
    
    public string SessionId { get; set; }
    
    public string Status { get; set; }
    
    public int MaxDefects { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public int? ProductionLineId { get; set; }
    
    public ProductionLine ProductionLine { get; set; }
    
    public int? CompanyId { get; set; }
    
    public Company Company { get; set; }
}
