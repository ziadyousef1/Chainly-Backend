using Chainly.Application.DTOs;
using Chainly.Application.DTOs.AiInsights;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces
{
    public interface IAiInsightsService
    {
        Task<IActionResult> StartSessionAsync(StartSessionRequestDto dto);
        Task<IActionResult> CloseSessionAsync(CloseSessionRequestDto dto);
    }
}
