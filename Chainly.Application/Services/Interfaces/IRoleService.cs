using Chainly.Application.DTOs.Role;

namespace Chainly.Application.Services.Interfaces;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetRolesAsync(int companyId);
    Task<(bool Success, string Message)> AddRoleAsync(string roleName, int companyId);
    Task<RolePermissionsDto?> GetPermissionsAsync(int roleId, int companyId);

    Task<(bool Success, string Message)>
        UpdatePermissionsAsync(int roleId, int companyId, string[] selectedPermissions);
}