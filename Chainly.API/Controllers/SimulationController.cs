using Chainly.Application.DTOs.Simulation;

namespace Chainly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController(ISimulationService simulationService) : BaseController
{
    [HttpPost("simulate")]
    [Authorize(Permissions.Simulation.Simulate)]
    public async Task<IActionResult> Simulate(SimulationRequestDto dto)
    {
        var companyId = GetCompanyId();
        return await simulationService.SimulateAsync(dto, companyId);
    }
}