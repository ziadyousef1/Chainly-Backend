using System.Security.Claims;
using Chainly.Application.DTOs.Role;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class RoleService(RoleManager<Role> roleManager, AppDbContext context) : IRoleService
{
    public async Task<IActionResult> GetAllRolesAsync(int companyId)
    {
        var roles = await roleManager.Roles
            .Where(r => r.CompanyId == companyId)
            .ToListAsync();

        var result = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
        });

        return new OkObjectResult(result);
    }


    public async Task<IActionResult> GetRoleAsync(int roleId, int companyId)
    {
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId);

        if (role is null)
            return new NotFoundObjectResult(new { message = "Role not found" });

        var roleClaims = await context.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == CustomClaimTypes.Permission)
            .Select(rc => rc.ClaimValue)
            .ToListAsync();

        var allPermissions = Permissions.GenerateAllPermissions();

        var permissionsDto = allPermissions.ToDictionary(
            g => g.Key,
            g => g.Value.Select(p => new PermissionDto
            {
                Name = p,
                IsAssigned = roleClaims.Contains(p)
            }).ToList()
        );

        var dto = new RoleUpdatePermissionsDto
        {
            Name = role.Name,
            Permissions = permissionsDto
        };

        return new OkObjectResult(dto);
    }

    public async Task<IActionResult> CreateRoleAsync(RoleCreateDto dto, int companyId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BadRequestObjectResult(new { message = "Role name is required" });

        var exists = await context.Roles
            .AnyAsync(r => r.Name == dto.Name && r.CompanyId == companyId);

        if (exists)
            return new BadRequestObjectResult(new { message = "Role already exists for this company" });

        var role = new Role
        {
            Name = dto.Name.Trim(),
            CompanyId = companyId,
            NormalizedName = dto.Name.Trim().ToUpper()
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Role created successfully", role.Id, role.Name });
    }
    public async Task<IActionResult> UpdateRoleAsync(int roleId, int companyId, RoleUpdatePermissionsDto dto)
    {
        var role = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId);

        if (role is null)
            return new NotFoundObjectResult(new { message = "Role not found" });

        role.Name = dto.Name.Trim();
        role.NormalizedName = dto.Name.Trim().ToUpper();
        await roleManager.UpdateAsync(role);


        var roleClaims = await roleManager.GetClaimsAsync(role);
        foreach (var claim in roleClaims)
            await roleManager.RemoveClaimAsync(role, claim);

        var selectedPermissions = dto.Permissions
            .SelectMany(g => g.Value)
            .Where(p => p.IsAssigned)
            .Select(p => p.Name)
            .ToList();

        foreach (var permission in selectedPermissions)
            await roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

        return new OkObjectResult(new { message = "Role updated successfully"});
    }


    public async Task<IActionResult> DeleteRoleAsync(int roleId, int companyId)
    {
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId);

        if (role is null)
            return new NotFoundObjectResult(new { message = "Role not found" });

        var roleClaims = await context.RoleClaims
            .Where(rc => rc.RoleId == role.Id)
            .ToListAsync();

        context.RoleClaims.RemoveRange(roleClaims);
        context.Roles.Remove(role);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Role deleted successfully" });
    }
}