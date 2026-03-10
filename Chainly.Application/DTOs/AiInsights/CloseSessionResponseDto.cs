using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs.AiInsights;

public class CloseSessionResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("report_id")]
    public string ReportId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}