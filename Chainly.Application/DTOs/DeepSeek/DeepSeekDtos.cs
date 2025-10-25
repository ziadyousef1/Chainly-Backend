using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs.DeepSeek;

public class DeepSeekMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class DeepSeekRequestDto
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }
    
    [JsonPropertyName("messages")]
    public required List<DeepSeekMessage> Messages { get; set; }
}

public class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public required DeepSeekMessage Message { get; set; }
}

public class DeepSeekResponseDto
{
    [JsonPropertyName("choices")]
    public required List<DeepSeekChoice> Choices { get; set; }
}