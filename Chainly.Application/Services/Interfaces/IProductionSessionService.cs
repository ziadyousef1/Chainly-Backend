using Chainly.Application.DTOs.ProductionSession;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IProductionSessionService
{
    Task<IActionResult> CreateSessionAsync(CreateProductionSessionDto request, int companyId);
}
