using Chainly.Application.DTOs.Role;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController(IRoleService roleService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Roles.View)]
    public async Task<IActionResult> GetAllRoles()
    {
        int companyId = GetCompanyId();
        return await roleService.GetAllRolesAsync(companyId);
    }


    [HttpPost]
    [Authorize(Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto model)
    {
        int companyId = GetCompanyId();
        return await roleService.CreateRoleAsync(model, companyId);
    }

    [HttpGet("{roleId}")]
    [Authorize(Permissions.Roles.View)]
    public async Task<IActionResult> GetRole(int roleId)
    {
        int companyId = GetCompanyId();
        return await roleService.GetRoleAsync(roleId, companyId);
    }

    [HttpPut("{roleId}")]
    [Authorize(Permissions.Roles.Update)]
    public async Task<IActionResult> UpdateRolesAsync(int roleId, [FromBody] RoleUpdatePermissionsDto model)
    {
        int companyId = GetCompanyId();
        return await roleService.UpdateRoleAsync(roleId, companyId, model);
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(int roleId)
    {
        int companyId = GetCompanyId();
        return await roleService.DeleteRoleAsync(roleId, companyId);
    }
}