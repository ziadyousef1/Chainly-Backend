using System.Text.Json.Serialization;

namespace Chainly.Application.DTOs.AzureOpenAI;

public class AzureOpenAIMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

public class AzureOpenAIRequestDto
{
    [JsonPropertyName("messages")]
    public required List<AzureOpenAIMessage> Messages { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 800;
    
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
    
    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.95;
}

public class AzureOpenAIChoice
{
    [JsonPropertyName("message")]
    public required AzureOpenAIMessage Message { get; set; }
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
    
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class AzureOpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class AzureOpenAIResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("object")]
    public string Object { get; set; }
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; }
    
    [JsonPropertyName("choices")]
    public required List<AzureOpenAIChoice> Choices { get; set; }
    
    [JsonPropertyName("usage")]
    public AzureOpenAIUsage Usage { get; set; }
}