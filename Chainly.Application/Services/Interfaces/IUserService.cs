using Chainly.Application.DTOs.Users;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetAllUsersAsync(int companyId, int pageNumber = 1, int pageSize = 10, bool? isActive = null,
            int? roleId = null, string? search = null);
        Task<IActionResult> GetUserByIdAsync(int id, int companyId);
        Task<IActionResult> CreateUserAsync(UserCreateDto dto, int companyId);
        Task<IActionResult> UpdateUserAsync(int id, UserUpdateDto dto, int companyId);
        Task<IActionResult> DeleteUserAsync(int id, int companyId);
        Task<IActionResult> GetCurrentUserAsync(int id);
        Task<IActionResult> ChangeUserActivationAsync(int id, int companyId, bool isActive);
    }
}