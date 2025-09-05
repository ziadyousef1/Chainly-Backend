using Chainly.API.Controllers;
using Chainly.Application.DTOs.Role;

namespace Chainly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController(IRoleService roleService) : BaseController
    {
        [HttpGet]
        [Authorize(Permissions.Roles.View)]
        public async Task<IActionResult> GetRoles()
        {
            int companyId = GetCompanyId();
            var roles = await roleService.GetRolesAsync(companyId);

            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
            });

            return Ok(result);
        }


        [HttpPost]
        [Authorize(Permissions.Roles.Create)]
        public async Task<IActionResult> AddRole([FromBody] RoleCreateDto model)
        {
            int companyId = GetCompanyId();
            var (success, message) = await roleService.AddRoleAsync(model.Name, companyId);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpGet("{roleId}/permissions")]
        [Authorize(Permissions.Roles.View)]
        public async Task<IActionResult> GetPermissions(int roleId)
        {
            int companyId = GetCompanyId();
            var result = await roleService.GetPermissionsAsync(roleId, companyId);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("{roleId}/permissions")]
        [Authorize(Permissions.Roles.Update)]
        public async Task<IActionResult> UpdatePermissions(int roleId, [FromBody] RoleUpdatePermissionsDto model)
        {
            int companyId = GetCompanyId();
            var (success, message) =
                await roleService.UpdatePermissionsAsync(roleId, companyId, model.SelectedPermissions);

            if (!success)
                return NotFound(new { message });

            return Ok(new { message });
        }
    }
}