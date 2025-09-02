namespace Chainly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Permissions.Users.View)]
        public async Task<IActionResult> GetAllUsers()
        {
            return await _userService.GetAllUsersAsync();
        }

        [HttpGet("{id}/roles")]
        [Authorize(Permissions.Users.View)]
        public async Task<IActionResult> GetUserRoles(int id)
        {
            return await _userService.GetUserRolesAsync(id);
        }

        [HttpPut("{id}/roles")]
        [Authorize(Permissions.Users.Update)]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] List<string> rolesToAssign)
        {
            return await _userService.UpdateUserRolesAsync(id, rolesToAssign);
        }
    }
}