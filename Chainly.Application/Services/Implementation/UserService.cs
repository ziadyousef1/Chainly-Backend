using Chainly.Application.DTOs.User;
using Chainly.Application.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services
{
    public class UserService(UserManager<User> userManager, AppDbContext context) : IUserService
    {
        public async Task<IActionResult> GetAllUsersAsync(int companyId, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = context.Users
                .Where(u => u.CompanyId == companyId);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            var result = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = userDtos
            };

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> CreateUserAsync(UserCreateDto dto, int companyId)
        {
            if (dto.Password != dto.ConfirmPassword)
                return new BadRequestObjectResult("Password and confirmation do not match");

            var existingUser = await userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return new BadRequestObjectResult("Email already exists");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                CompanyId = companyId,
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return new BadRequestObjectResult(result.Errors);

            if (dto.Roles != null && dto.Roles.Any())
            {
                foreach (var roleName in dto.Roles)
                {
                    var role = await context.Roles
                        .FirstOrDefaultAsync(r => r.Name == roleName && r.CompanyId == companyId);

                    if (role != null)
                    {
                        context.UserRoles.Add(new IdentityUserRole<int>
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        });
                    }
                }
            }

            return new OkObjectResult(new { Message = "User created successfully", UserId = user.Id });
        }

        public async Task<IActionResult> UpdateUserAsync(int id, UserUpdateDto dto, int companyId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
                return new NotFoundObjectResult("User not found");

            user.FullName = dto.FullName ?? user.FullName;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != user.Id);
                if (existingUser != null)
                    return new BadRequestObjectResult("Email is already taken by another user");

                user.Email = dto.Email;
                user.UserName = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.Password))
            {
                if (string.IsNullOrEmpty(dto.ConfirmPassword) || dto.Password != dto.ConfirmPassword)
                    return new BadRequestObjectResult("Password and confirmation do not match");

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, dto.Password);
                if (!result.Succeeded)
                    return new BadRequestObjectResult(result.Errors);
            }

            if (dto.Roles != null)
            {
                var validRoles = await context.Roles
                    .Where(r => r.CompanyId == companyId && dto.Roles.Contains(r.Name))
                    .ToListAsync();

                var currentUserRoles = await context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync();

                var rolesToRemove = currentUserRoles
                    .Where(ur => validRoles.All(vr => vr.Id != ur.RoleId))
                    .ToList();

                if (rolesToRemove.Any())
                    context.UserRoles.RemoveRange(rolesToRemove);

                var rolesToAdd = validRoles
                    .Where(vr => currentUserRoles.All(ur => ur.RoleId != vr.Id))
                    .Select(vr => new IdentityUserRole<int>
                    {
                        UserId = user.Id,
                        RoleId = vr.Id
                    })
                    .ToList();

                if (rolesToAdd.Any())
                    await context.UserRoles.AddRangeAsync(rolesToAdd);
            }

            await context.SaveChangesAsync();
            return new OkObjectResult("User updated successfully");
        }

        public async Task<IActionResult> DeleteUserAsync(int id, int companyId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
                return new NotFoundObjectResult("User not found");

            await userManager.DeleteAsync(user);
            return new OkObjectResult("User deleted successfully");
        }

        public async Task<IActionResult> GetUserByIdAsync(int id, int companyId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (user == null)
                return new NotFoundObjectResult("User not found");

            var roles = await userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles.ToList()
            };

            return new OkObjectResult(userDto);
        }

        public async Task<IActionResult> GetUserRolesAsync(int id)
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return new NotFoundObjectResult("User not found");

            var roles = await userManager.GetRolesAsync(user);
            return new OkObjectResult(roles);
        }

        public async Task<IActionResult> UpdateUserRolesAsync(int id, List<string> rolesToAssign)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return new NotFoundObjectResult("User not found");

            var validRoles = await context.Roles
                .Where(r => r.CompanyId == user.CompanyId && rolesToAssign.Contains(r.Name))
                .ToListAsync();

            var currentUserRoles = await context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();

            var rolesToRemove = currentUserRoles
                .Where(ur => !validRoles.Any(vr => vr.Id == ur.RoleId))
                .ToList();
            if (rolesToRemove.Any())
                context.UserRoles.RemoveRange(rolesToRemove);

            var rolesToAdd = validRoles
                .Where(vr => !currentUserRoles.Any(ur => ur.RoleId == vr.Id))
                .Select(vr => new IdentityUserRole<int>
                {
                    UserId = user.Id,
                    RoleId = vr.Id
                })
                .ToList();
            if (rolesToAdd.Any())
                await context.UserRoles.AddRangeAsync(rolesToAdd);

            await context.SaveChangesAsync();
            return new OkObjectResult("User roles updated successfully");
        }
    }
}