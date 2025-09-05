using Chainly.Application.DTOs.User;
namespace Chainly.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Users.View)]
    public async Task<IActionResult> GetAllUsers()
    {
        int companyId = GetCompanyId();
        return await userService.GetAllUsersAsync(companyId);
    }

    [HttpGet("{id}")]
    [Authorize(Permissions.Users.View)]
    public async Task<IActionResult> GetUser(int id)
    {
        int companyId = GetCompanyId();
        return await userService.GetUserByIdAsync(id, companyId);
    }

    [HttpPost]
    [Authorize(Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        int companyId = GetCompanyId();
        return await userService.CreateUserAsync(dto, companyId);
    }

    [HttpPut("{id}")]
    [Authorize(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
    {
        int companyId = GetCompanyId();
        return await userService.UpdateUserAsync(id, dto, companyId);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        int companyId = GetCompanyId();
        return await userService.DeleteUserAsync(id, companyId);
    }

    [HttpGet("{id}/roles")]
    [Authorize(Permissions.Users.View)]
    public async Task<IActionResult> GetUserRoles(int id)
    {
        return await userService.GetUserRolesAsync(id);
    }

    [HttpPut("{id}/roles")]
    [Authorize(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] List<string> rolesToAssign)
    {
        return await userService.UpdateUserRolesAsync(id, rolesToAssign);
    }
}