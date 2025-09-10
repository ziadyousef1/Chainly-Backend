using Chainly.Application.DTOs.Users;
namespace Chainly.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : BaseController
{
    [HttpGet]
    [Authorize(Permissions.Users.View)]
    public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        int companyId = GetCompanyId();
        return await userService.GetAllUsersAsync(companyId, pageNumber, pageSize);
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
}