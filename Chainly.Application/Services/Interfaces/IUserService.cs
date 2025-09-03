using Chainly.Application.DTOs.User;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetAllUsersAsync(int companyId);
        Task<IActionResult> GetUserByIdAsync(int id, int companyId);
        Task<IActionResult> CreateUserAsync(UserCreateDto dto, int companyId);
        Task<IActionResult> UpdateUserAsync(int id, UserUpdateDto dto, int companyId);
        Task<IActionResult> DeleteUserAsync(int id, int companyId);
        Task<IActionResult> GetUserRolesAsync(int id);
        Task<IActionResult> UpdateUserRolesAsync(int id, List<string> rolesToAssign);
    }
}
