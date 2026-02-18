using Chainly.Application.DTOs.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Services.Interfaces;

public interface IRoleService
{
    Task<IActionResult> GetAllRolesAsync(int companyId, int pageNumber = 1, int pageSize = 10, string? search = null);
    Task<IActionResult> GetRoleByIdAsync(int roleId, int companyId);
    Task<IActionResult> CreateRoleAsync(RoleCreateDto dto, int companyId);
    Task<IActionResult> UpdateRoleAsync(int roleId, int companyId, RoleUpdatePermissionsDto dto);
    Task<IActionResult> DeleteRoleAsync(int roleId, int companyId);

}