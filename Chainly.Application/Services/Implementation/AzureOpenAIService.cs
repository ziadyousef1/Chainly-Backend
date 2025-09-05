using Azure;
using Azure.AI.OpenAI;
using Chainly.Application.Configurations;
using Chainly.Application.Services.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Chainly.Application.Services.Implementation;

public class AzureOpenAIService:IAzureOpenAIService
{
    private readonly AzureOpenAISettings _azureOpenAiSettings;

    public AzureOpenAIService(IOptions<AzureOpenAISettings> azureOpenAISettings)
    {
        _azureOpenAiSettings = azureOpenAISettings.Value;
    }
    public async Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate)
    {
      
        var systemPrompt = @"You are a helpful customer service assistant for Chainly, a supply chain management company. 
            Provide friendly, informative updates about customer orders. Keep responses concise and professional.
            Include relevant details like status, delivery dates, and tracking information when available.";
        
        var userPrompt = $@"Generate a customer-friendly update message for this order:
            {orderUpdate}
            Ensure the message is clear, polite, and provides useful information about the order status.";
        var endpoint = new Uri(_azureOpenAiSettings.Endpoint);
        var deploymentName = _azureOpenAiSettings.DeploymentName;
        var apiKey = _azureOpenAiSettings.ApiKey;

        AzureOpenAIClient azureClient = new(
            endpoint,
            new AzureKeyCredential(apiKey));
        ChatClient chatClient = azureClient.GetChatClient(deploymentName);

        var requestOptions = new ChatCompletionOptions()
        {
            Temperature = 1.0f,
            TopP = 1.0f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f,
    
        };

        List<ChatMessage> messages = new List<ChatMessage>()
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt),
        };

        var response = chatClient.CompleteChat(messages, requestOptions);
        return response.Value.Content[0].Text;
    }
}