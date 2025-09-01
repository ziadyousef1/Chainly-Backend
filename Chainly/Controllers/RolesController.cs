using Chainly.Application.DTOs.Role;

namespace Chainly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [Authorize(Permissions.Roles.View)]
        public async Task<IActionResult> GetRoles()
        {
            int companyId = GetCompanyIdFromToken();
            var roles = await _roleService.GetRolesAsync(companyId);

            var result = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                CompanyId = r.CompanyId
            });

            return Ok(result);
        }


        [HttpPost]
        [Authorize(Permissions.Roles.Create)]
        public async Task<IActionResult> AddRole([FromBody] RoleCreateDto model)
        {
            int companyId = GetCompanyIdFromToken();
            var (success, message) = await _roleService.AddRoleAsync(model.Name, companyId);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpGet("{roleId}/permissions")]
        [Authorize(Permissions.Roles.View)]
        public async Task<IActionResult> GetPermissions(int roleId)
        {
            int companyId = GetCompanyIdFromToken();
            var result = await _roleService.GetPermissionsAsync(roleId, companyId);

            if (result is null)
                return NotFound();

            var dto = new RolePermissionsDto
            {
                RoleId = result.RoleId,
                RoleName = result.RoleName,
                Permissions = result.Permissions.Select(p => new PermissionDto
                {
                    Name = p.Name,
                    IsAssigned = p.IsAssigned
                })
            };

            return Ok(dto);
        }

        [HttpPut("{roleId}/permissions")]
        [Authorize(Permissions.Roles.Update)]
        public async Task<IActionResult> UpdatePermissions(int roleId, [FromBody] RoleUpdatePermissionsDto model)
        {
            int companyId = GetCompanyIdFromToken();
            var (success, message) =
                await _roleService.UpdatePermissionsAsync(roleId, companyId, model.SelectedPermissions);

            if (!success)
                return NotFound(new { message });

            return Ok(new { message });
        }

        private int GetCompanyIdFromToken()
        {
            var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
            if (companyIdClaim == null)
                throw new UnauthorizedAccessException("CompanyId not found in token.");

            return int.Parse(companyIdClaim);
        }
    }
}