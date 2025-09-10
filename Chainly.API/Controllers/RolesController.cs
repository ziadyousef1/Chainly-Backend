using Chainly.Application.DTOs.Roles;
using Chainly.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController(IRoleService roleService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Roles.View)]
    public async Task<IActionResult> GetAllRoles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await roleService.GetAllRolesAsync(companyId, pageNumber, pageSize);
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
        return await roleService.GetRoleByIdAsync(roleId, companyId);
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