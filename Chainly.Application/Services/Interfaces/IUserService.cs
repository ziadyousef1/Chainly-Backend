using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetAllUsersAsync();
        Task<IActionResult> GetUserRolesAsync(int id);
        Task<IActionResult> UpdateUserRolesAsync(int id, List<string> rolesToAssign);
    }
}
