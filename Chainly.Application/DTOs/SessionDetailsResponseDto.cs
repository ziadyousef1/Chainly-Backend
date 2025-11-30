using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs
{
    public class SessionDetailsResponseDto
    {
        [JsonPropertyName("session_id")] public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("total_products")] public int TotalProducts { get; set; }

        [JsonPropertyName("defective_count")] public int DefectiveCount { get; set; }

        [JsonPropertyName("video_name")] public string VideoName { get; set; } = string.Empty;

        [JsonPropertyName("non_defective_count")]
        public int NonDefectiveCount { get; set; }

        [JsonPropertyName("defective_products")]
        public List<DefectiveProductDto> DefectiveProducts { get; set; } = new();
    }

    public class DefectiveProductDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

        [JsonPropertyName("detected_at")] public string DetectedAt { get; set; } = string.Empty;
    }
    public class NonDefectiveProductDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    }
}