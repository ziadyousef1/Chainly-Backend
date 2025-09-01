using Chainly.Application.Interfaces;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserService(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FullName
                })
                .ToListAsync();

            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var userEntity = await _userManager.FindByIdAsync(user.Id.ToString());
                if (userEntity != null)
                {
                    var roles = await _roleManager.Roles
                        .Where(r => r.CompanyId == userEntity.CompanyId)
                        .Select(r => r.Name)
                        .ToListAsync();

                    var userRoles = await _userManager.GetRolesAsync(userEntity);

                    var filteredUserRoles = userRoles.Intersect(roles);

                    usersWithRoles.Add(new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.FullName,
                        Roles = filteredUserRoles
                    });
                }
            }

            return new OkObjectResult(usersWithRoles);
        }

        public async Task<IActionResult> GetUserRolesAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return new NotFoundObjectResult("User not found");

            var allRoles = await _roleManager.Roles
                .Where(r => r.CompanyId == user.CompanyId)
                .ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var filteredUserRoles = userRoles.Intersect(allRoles.Select(r => r.Name));

            var result = allRoles.Select(r => new
            {
                RoleName = r.Name,
                IsSelected = filteredUserRoles.Contains(r.Name ?? "")
            });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> UpdateUserRolesAsync(int id, List<string> rolesToAssign)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return new NotFoundObjectResult("User not found");

            var validRoles = await _roleManager.Roles
                .Where(r => r.CompanyId == user.CompanyId)
                .Select(r => r.Name)
                .ToListAsync();

            var filteredRolesToAssign = rolesToAssign.Intersect(validRoles).ToList();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(filteredRolesToAssign);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            var rolesToAdd = filteredRolesToAssign.Except(currentRoles);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            return new NoContentResult();
        }
    }
}
