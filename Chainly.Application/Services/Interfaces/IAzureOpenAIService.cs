namespace Chainly.Application.Services.Interfaces;

public interface IAzureOpenAIService
{
    Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate);

}