using System.Text;
using System.Text.Json;
using Chainly.Application.Configurations;
using Chainly.Application.DTOs.DeepSeek;
using Chainly.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation;

public class DeepSeekService : IDeepSeekService
{
    private readonly DeepSeekSettings _deepSeekSettings;
    private readonly HttpClient _httpClient;

    public DeepSeekService(IOptions<DeepSeekSettings> deepSeekSettings, HttpClient httpClient)
    {
        _deepSeekSettings = deepSeekSettings.Value;
        _httpClient = httpClient;
    }

    public async Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate)
    {
        var systemPrompt = @"You are a customer service assistant for Chainly supply chain management company. 
            Generate ONLY a direct customer update message. No thinking, no explanations, no placeholders like [Customer Name].
            
            Format exactly like this example:
            'Dear Customer, Your order is currently [status]. As of [date], it remains in this stage. We appreciate your patience and will provide further updates as soon as more information is available. Thank you for choosing Chainly. Best regards, Customer Service Team, Chainly'
            
            Replace [status] and [date] with actual information from the order data. Keep it simple, professional, and under 80 words.";
        
        var userPrompt = $@"Generate a customer update message using this order information: {orderUpdate}
            
            Write ONLY the message text - no explanations, no thinking process, no extra formatting. 
            Use 'Dear Customer' as greeting and end with 'Best regards, Customer Service Team, Chainly'.
            Include the actual order status and any relevant dates from the provided data.";

        var request = new DeepSeekRequestDto
        {
            Model = _deepSeekSettings.Model,
            Messages = new List<DeepSeekMessage>
            {
                new DeepSeekMessage { Role = "system", Content = systemPrompt },
                new DeepSeekMessage { Role = "user", Content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_deepSeekSettings.BearerToken}");

        var response = await _httpClient.PostAsync(_deepSeekSettings.Url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var deepSeekResponse = JsonSerializer.Deserialize<DeepSeekResponseDto>(responseJson);

        var messageContent = deepSeekResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response available";
        
        // Clean up any unwanted content like <think> tags or meta-commentary
        messageContent = CleanupResponse(messageContent);
        
        return messageContent;
    }
    
    private string CleanupResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
            return "Dear Customer, This is an update on your order. We are currently processing your request and will provide further updates soon. Thank you for choosing Chainly. Best regards, Customer Service Team, Chainly";
            
        // Remove <think> tags and their entire content
        while (response.Contains("<think>") && response.Contains("</think>"))
        {
            var thinkStart = response.IndexOf("<think>");
            var thinkEnd = response.IndexOf("</think>") + 8;
            if (thinkStart >= 0 && thinkEnd > thinkStart)
            {
                response = response.Remove(thinkStart, thinkEnd - thinkStart).Trim();
            }
            else break;
        }
        
        // Remove markdown formatting and meta-commentary
        response = response
            .Replace("**", "") // Remove bold markdown
            .Replace("---", "") // Remove separators
            .Replace("\n\n", " ") // Replace double newlines with space
            .Replace("\n", " "); // Replace single newlines with space
        
        // Remove lines that contain meta-commentary
        var cleanedLines = response.Split(' ')
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();
            
        var finalResponse = string.Join(" ", cleanedLines).Trim();
        
        // If response doesn't start with "Dear Customer", ensure it's properly formatted
        if (!finalResponse.StartsWith("Dear Customer", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the core message and reformat it
            var coreMessage = ExtractCoreMessage(finalResponse);
            return coreMessage;
        }
        
        return finalResponse;
    }
    
    private string ExtractCoreMessage(string response)
    {
        // If the AI response doesn't follow our format, create a generic but informative message
        if (response.Length < 20 || response.Contains("I'll") || response.Contains("Here's"))
        {
            return "Dear Customer, This is an update on your order. We are currently processing your request and will provide further updates as soon as more information becomes available. Thank you for choosing Chainly. Best regards, Customer Service Team, Chainly";
        }
        
        return response.Trim();
    }
}