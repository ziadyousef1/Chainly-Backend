using Chainly.Application.DTOs.Simulation;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface ISimulationService
{
    Task<IActionResult> SimulateAsync(SimulationRequestDto dto, int companyId);
}