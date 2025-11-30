using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs.ProductionSession;

public class CreateProductionSessionDto
{
    [Required]
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; }
    
    [Required]
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }
    
    [Required]
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [Required]
    [JsonPropertyName("max_defects")]
    public int MaxDefects { get; set; }
    
    [Required]
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("productionline_id")]
    public int ProductionLineId { get; set; }
    
    public int CompanyId { get; set; }
}
