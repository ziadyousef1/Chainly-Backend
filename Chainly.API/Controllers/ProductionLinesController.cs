using Chainly.Application.DTOs.ProductionLines;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductionLinesController(IProductionLineService productionLineService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.ProductionLines.View)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await productionLineService.GetAllAsync(companyId, pageNumber, pageSize);
    }

    [HttpPost]
    [Authorize(Permissions.ProductionLines.Create)]
    public async Task<IActionResult> Create([FromBody] ProductionLineCreateDto model)
    {
        int companyId = GetCompanyId();
        return await productionLineService.CreateAsync(model, companyId);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.ProductionLines.View)]
    public async Task<IActionResult> Get(int id)
    {
        int companyId = GetCompanyId();
        return await productionLineService.GetAsync(id, companyId);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.ProductionLines.Update)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductionLineUpdateDto model)
    {
        int companyId = GetCompanyId();
        return await productionLineService.UpdateAsync(id, companyId, model);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.ProductionLines.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        int companyId = GetCompanyId();
        return await productionLineService.DeleteAsync(id, companyId);
    }
}