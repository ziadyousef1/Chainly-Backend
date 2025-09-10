using Chainly.Application.DTOs;

namespace Chainly.Application.Services.Interfaces
{
    public interface IAiInsightsService
    {
        Task<SessionDetailsResponseDto?> GetSessionDetailsAsync(string sessionId);
    }
}
