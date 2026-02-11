using Chainly.Application.DTOs.Users;
namespace Chainly.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Users.View)]
    public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10,  bool? isActive = null, int? roleId = null)
    {
        int companyId = GetCompanyId();
        return await userService.GetAllUsersAsync(companyId, pageNumber, pageSize, isActive, roleId);
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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        int userId = GetUserId();

        return await userService.GetCurrentUserAsync(userId);
    }
    
    [HttpPatch("{id}/activation")]
    [Authorize(Permissions.Users.Activate)]
    public async Task<IActionResult> ChangeActivation(int id, bool isActive)
    {
        int companyId = GetCompanyId();
        return await userService.ChangeUserActivationAsync(id, companyId, isActive);
    }
}