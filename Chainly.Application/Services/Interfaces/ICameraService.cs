using Chainly.Application.DTOs.Cameras;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface ICameraService
{
    Task<IActionResult> CreateCameraAsync(int companyId, CreateCameraDto dto);
    Task<IActionResult> UpdateCameraAsync(int companyId, int cameraId, UpdateCameraDto dto);
    Task<IActionResult> DeleteCameraAsync(int companyId, int cameraId);
    Task<IActionResult> GetCamerasAsync(int companyId, int? productionLineId = null, string? search = null,
        int pageNumber = 1, int pageSize = 10);
}