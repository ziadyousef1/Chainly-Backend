using Chainly.Application.DTOs.Cameras;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CamerasController(ICameraService cameraService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Cameras.View)]
    public async Task<IActionResult> GetAll(int? productionLineId, string? search, int pageNumber = 1, int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await cameraService.GetCamerasAsync(companyId, productionLineId, search, pageNumber, pageSize);
    }

    [HttpPost]
    [Authorize(Permissions.Cameras.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCameraDto dto)
    {
        int companyId = GetCompanyId();
        return await cameraService.CreateCameraAsync(companyId, dto);
    }

    [HttpPut("{cameraId}")]
    [Authorize(Permissions.Cameras.Update)]
    public async Task<IActionResult> Update(int cameraId, [FromBody] UpdateCameraDto dto)
    {
        int companyId = GetCompanyId();
        return await cameraService.UpdateCameraAsync(companyId, cameraId, dto);
    }

    [HttpDelete("{cameraId}")]
    [Authorize(Permissions.Cameras.Delete)]
    public async Task<IActionResult> Delete(int cameraId)
    {
        int companyId = GetCompanyId();
        return await cameraService.DeleteCameraAsync(companyId, cameraId);
    }
}