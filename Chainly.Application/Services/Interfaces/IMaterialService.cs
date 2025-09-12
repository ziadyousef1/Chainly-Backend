using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IMaterialService
{
    Task<IActionResult> GetAllMaterialsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        string? sortOrder = null, string? search = null);

    Task<IActionResult> GetMaterialByIdAsync(int id, int companyId, int pageNumber = 1, int pageSize = 10,
        string? sortOrder = null, string? search = null);
}