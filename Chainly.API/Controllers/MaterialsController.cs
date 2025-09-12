namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaterialsController(IMaterialService materialService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Materials.View)]
    public async Task<IActionResult> GetAllMaterials(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortOrder = null,
        [FromQuery] string? search = null)
    {
        int companyId = GetCompanyId();
        return await materialService.GetAllMaterialsAsync(companyId, pageNumber, pageSize, sortOrder, search);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.Materials.View)]
    public async Task<IActionResult> GetMaterial(int id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? sortOrder = null, [FromQuery] string? search = null)
    {
        int companyId = GetCompanyId();
        return await materialService.GetMaterialByIdAsync(id, companyId, pageNumber, pageSize, sortOrder, search);
    }
}