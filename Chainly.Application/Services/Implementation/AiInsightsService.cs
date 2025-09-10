using Chainly.Application.Configurations;
using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation
{
    public class AiInsightsService : IAiInsightsService
    {
        private readonly IExternalApiService _externalApiService;
        private readonly AiApiSettings _aiApiSettings;

        public AiInsightsService(IExternalApiService externalApiService, IOptions<AiApiSettings> aiApiSettings)
        {
            _externalApiService = externalApiService;
            _aiApiSettings = aiApiSettings.Value;
        }

        public async Task<SessionDetailsResponseDto?> GetSessionDetailsAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            var url = $"{_aiApiSettings.BaseUrl}/session-details";
            var result = await _externalApiService.GetByIdAsync<SessionDetailsResponseDto>(url, sessionId);
            
            return result;
        }
    }
}
