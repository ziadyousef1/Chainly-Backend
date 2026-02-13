using Chainly.Application.DTOs.Users;
using Chainly.Application.Interfaces;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class UserService(UserManager<User> userManager, AppDbContext context, IHttpContextAccessor accessor) : IUserService
{
    public async Task<IActionResult> GetAllUsersAsync(
        int companyId,
        int pageNumber = 1,
        int pageSize = 10,
        bool? isActive = null,
        int? roleId = null,
        string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var usersQuery = context.Users
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();

            usersQuery = usersQuery.Where(u =>
                u.FullName.Contains(trimmedSearch) ||
                u.Email.Contains(trimmedSearch));
        }

        if (isActive.HasValue)
            usersQuery = usersQuery.Where(u => u.IsActive == isActive.Value);

        if (roleId.HasValue)
        {
            usersQuery = usersQuery
                .Where(u => context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == roleId.Value));
        }

        var activeUsersCount = await context.Users
            .CountAsync(u => u.CompanyId == companyId && u.IsActive);

        var inactiveUsersCount = await context.Users
            .CountAsync(u => u.CompanyId == companyId && !u.IsActive);

        var totalCount = await usersQuery.CountAsync();

        var pagedUsers = await usersQuery
            .OrderBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = pagedUsers.Select(u => u.Id).ToList();

        var rolesLookup = await (
            from userRole in context.UserRoles
            join role in context.Roles
                on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                role.Name
            }
        ).ToListAsync();

        var groupedRoles = rolesLookup
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Name!)
                      .Where(name => !string.IsNullOrEmpty(name))
                      .ToList()
            );

        var userDtos = pagedUsers.Select(user => new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = groupedRoles.ContainsKey(user.Id)
                ? groupedRoles[user.Id]
                : new List<string>()
        }).ToList();

        var result = new
        {
            TotalCount = totalCount,
            ActiveUsersCount = activeUsersCount,
            InactiveUsersCount = inactiveUsersCount,
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
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        if (dto.Roles is not null && dto.Roles.Any())
        {
            var roles = await context.Roles
                .Where(r => dto.Roles.Contains(r.Name) && r.CompanyId == companyId)
                .Select(r => r.Id)
                .ToListAsync();

            if (roles.Any())
            {
                var existingUserRoleIds = await context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                var newUserRoles = roles
                    .Where(roleId => !existingUserRoleIds.Contains(roleId))
                    .Select(roleId => new IdentityUserRole<int>
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });

                await context.UserRoles.AddRangeAsync(newUserRoles);
                await context.SaveChangesAsync();
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
            user.IsActive = dto.IsActive.Value;
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
            Roles = roles.ToList(),
            IsActive = user.IsActive,
        };

        return new OkObjectResult(userDto);
    }

    public async Task<IActionResult> GetCurrentUserAsync(int userId)
    {
        var user = await context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        var userProfile = new UserProfileDto
        {
            ProfilePicture = user.ProfilePicture ?? AvatarHelper.GetDefaultAvatar(accessor),
            Name = user.FullName,
            Email = user.Email,
            CompanyName = user.Company?.Name,
        };

        return new OkObjectResult(userProfile);
    }
    
    public async Task<IActionResult> ChangeUserActivationAsync(int id, int companyId, bool isActive)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

        if (user == null)
            return new NotFoundObjectResult("User not found");

        if (user.IsActive == isActive)
            return new BadRequestObjectResult(
                $"User is already {(isActive ? "active" : "inactive")}");

        user.IsActive = isActive;

        await context.SaveChangesAsync();

        return new OkObjectResult(new
        {
            Message = $"User {(isActive ? "activated" : "deactivated")} successfully",
            UserId = user.Id,
            IsActive = user.IsActive
        });
    }
}