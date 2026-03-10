using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs.AiInsights;

public class OpenReportResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("report_id")]
    public string ReportId { get; set; } = default!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;
}