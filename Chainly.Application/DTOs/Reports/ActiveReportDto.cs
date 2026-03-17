namespace Chainly.Application.DTOs.Reports;
using System.Text.Json.Serialization;

public class ActiveReportDto
{
    [JsonPropertyName("report_id")]
    public string ReportId { get; set; } = string.Empty;

    [JsonPropertyName("viewers_count")]
    public int ViewersCount { get; set; }
}