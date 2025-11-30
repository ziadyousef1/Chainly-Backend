using Chainly.Application.DTOs.ProductionLines;
using Chainly.Application.DTOs.ProductionSession;
using Chainly.Application.Services.Interfaces;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductionLinesController(IProductionLineService productionLineService, IProductionSessionService sessionService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.ProductionLines.View)]
    public async Task<IActionResult> GetAllProductionLines([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await productionLineService.GetAllProductionLinesAsync(companyId, pageNumber, pageSize);
    }

    [HttpPost]
    [Authorize(Permissions.ProductionLines.Create)]
    public async Task<IActionResult> CreateProductionLine([FromBody] ProductionLineCreateDto model)
    {
        int companyId = GetCompanyId();
        return await productionLineService.CreateProductionLineAsync(model, companyId);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.ProductionLines.View)]
    public async Task<IActionResult> GetProductionLine(int id)
    {
        int companyId = GetCompanyId();
        return await productionLineService.GetProductionLineByIdAsync(id, companyId);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.ProductionLines.Update)]
    public async Task<IActionResult> UpdateProductionLine(int id, [FromBody] ProductionLineUpdateDto model)
    {
        int companyId = GetCompanyId();
        return await productionLineService.UpdateProductionLineAsync(id, companyId, model);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.ProductionLines.Delete)]
    public async Task<IActionResult> DeleteProductionLine(int id)
    {
        int companyId = GetCompanyId();
        return await productionLineService.DeleteProductionLineAsync(id, companyId);
    }

    [HttpPost("sessions")]
//    [Authorize(Permissions.ProductionLines.Create)]
    public async Task<IActionResult> CreateSession( [FromBody] CreateProductionSessionDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        return await sessionService.CreateSessionAsync(request, request.CompanyId);
    }
}