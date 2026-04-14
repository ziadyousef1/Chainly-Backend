using System.Text;
using System.Text.Json;
using Chainly.Application.Configurations;
using Chainly.Application.DTOs.AzureOpenAI;
using Chainly.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation;

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAISettings _azureOpenAISettings;
    private readonly HttpClient _httpClient;

    public AzureOpenAIService(IOptions<AzureOpenAISettings> azureOpenAISettings, HttpClient httpClient)
    {
        _azureOpenAISettings = azureOpenAISettings.Value;
        _httpClient = httpClient;
    }

    public async Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate)
    {
        var systemPrompt = @"You are a customer service assistant for Chainly supply chain management company. 
            Generate ONLY a direct customer update message. No thinking, no explanations, no placeholders like [Customer Name].
            
            Format exactly like this example:§
            'Dear Customer, Your order is currently [status]. As of [date], it remains in this stage. We appreciate your patience and will provide further updates as soon as more information is available. Thank you for choosing Chainly. Best regards, Customer Service Team, Chainly'
            
            Replace [status] and [date] with actual information from the order data. Keep it simple, professional, and under 80 words.";

        var userPrompt = $@"Generate a customer update message using this order information: {orderUpdate}
            
            Write ONLY the message text - no explanations, no thinking process, no extra formatting. 
            Use 'Dear Customer' as greeting and end with 'Best regards, Customer Service Team, Chainly'.
            Include the actual order status and any relevant dates from the provided data.";

        var request = new AzureOpenAIRequestDto
        {
            Messages = new List<AzureOpenAIMessage>
            {
                new AzureOpenAIMessage { Role = "system", Content = systemPrompt },
                new AzureOpenAIMessage { Role = "user", Content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAISettings.ApiKey);

        var url =
            $"{_azureOpenAISettings.Endpoint}/openai/deployments/{_azureOpenAISettings.DeploymentName}/chat/completions?api-version={_azureOpenAISettings.ApiVersion}";

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var azureOpenAIResponse = JsonSerializer.Deserialize<AzureOpenAIResponseDto>(responseJson);

        var messageContent =
            azureOpenAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response available";

        // Clean up any unwanted content like <think> tags or meta-commentary

        return messageContent;
    }


    private string ExtractCoreMessage(string response)
    {
        if (response.Length < 20 || response.Contains("I'll") || response.Contains("Here's"))
        {
            return
                "Dear Customer, This is an update on your order. We are currently processing your request and will provide further updates as soon as more information becomes available. Thank you for choosing Chainly. Best regards, Customer Service Team, Chainly";
        }

        return response.Trim();
    }

    public async Task<string> GenerateSimulationAnalysisAsync(object simulationResult)
    {
        var simulationJson = JsonSerializer.Serialize(simulationResult, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var systemPrompt = @"You are an industrial AI assistant for Chainly's Digital Twin platform.
Analyze production line simulation results and provide a concise and professional assessment.

Rules:
- Return ONLY plain text.
- Do NOT include explanations, thinking steps, or formatting symbols.
- Keep the response under 120 words.
- Clearly state whether the operation is feasible or not.
- Provide brief recommendations if improvements are needed.";

        var userPrompt = $@"
Analyze the following production line simulation results:

{simulationJson}

Provide:
1. A summary of performance.
2. The operational decision (OK or FAIL).
3. Recommendations for optimization if necessary.

Return only the final analysis as plain text.";

        var request = new AzureOpenAIRequestDto
        {
            Messages = new List<AzureOpenAIMessage>
            {
                new AzureOpenAIMessage { Role = "system", Content = systemPrompt },
                new AzureOpenAIMessage { Role = "user", Content = userPrompt }
            },
            Temperature = 0.2,
            MaxTokens = 200
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _azureOpenAISettings.ApiKey);

        var url =
            $"{_azureOpenAISettings.Endpoint}/openai/deployments/{_azureOpenAISettings.DeploymentName}/chat/completions?api-version={_azureOpenAISettings.ApiVersion}";

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var azureOpenAIResponse =
            JsonSerializer.Deserialize<AzureOpenAIResponseDto>(responseJson);

        var messageContent =
            azureOpenAIResponse?.Choices?.FirstOrDefault()?.Message?.Content
            ?? "No analysis available.";

        return ExtractPlainText(messageContent);
    }

    private string ExtractPlainText(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "No analysis available.";

        response = System.Text.RegularExpressions.Regex
            .Replace(response, "<think>.*?</think>", string.Empty,
                System.Text.RegularExpressions.RegexOptions.Singleline);

        return response.Trim();
    }
}