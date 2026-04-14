namespace Chainly.Application.Services.Interfaces;

public interface IAzureOpenAIService
{
    Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate);
    Task<string> GenerateSimulationAnalysisAsync(object simulationResult);
}