namespace Chainly.Application.Services.Interfaces;

public interface IDeepSeekService
{
    Task<string> GenerateOrderUpdateMessageAsync(string orderUpdate);

}