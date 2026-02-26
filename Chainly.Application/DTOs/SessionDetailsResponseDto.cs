using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs
{
    public class SessionDetailsResponseDto
    {
        [JsonPropertyName("defective_products")] public int DefectiveProducts { get; set; }
        [JsonPropertyName("good_products")] public int GoodProducts { get; set; }
        [JsonPropertyName("video_name")] public string VideoName { get; set; } = string.Empty;
    }
}