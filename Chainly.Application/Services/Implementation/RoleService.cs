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

public class RoleService : IRoleService
{
    private readonly RoleManager<Role> _roleManager;
    private readonly AppDbContext _context;

    public RoleService(RoleManager<Role> roleManager, AppDbContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync(int companyId)
    {
        var roles = await _roleManager.Roles
            .Where(r => r.CompanyId == companyId)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
        });
    }

    public async Task<(bool Success, string Message)> AddRoleAsync(string roleName, int companyId)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return (false, "Role name is required");

        var exists = await _roleManager.Roles
            .AnyAsync(r => r.Name == roleName && r.CompanyId == companyId);

        if (exists)
            return (false, "Role already exists for this company");

        var role = new Role
        {
            Name = roleName.Trim(),
            CompanyId = companyId,
            NormalizedName = roleName.Trim().ToUpper()
        };

        _context.Roles.Add(role);

        await _context.SaveChangesAsync();

        return (true, "Role created successfully");
    }

    public async Task<RolePermissionsDto?> GetPermissionsAsync(int roleId, int companyId)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId);

        if (role is null)
            return null;

        var roleClaims = await _roleManager.GetClaimsAsync(role);
        var permissions = Permissions.GenerateAllPermissions();

        var assignedPermissions = permissions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(p => new PermissionDto
            {
                Name = p,
                IsAssigned = roleClaims.Any(c => c.Type == CustomClaimTypes.Permission && c.Value == p)
            }).ToList()
        );

        return new RolePermissionsDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            Permissions = assignedPermissions
        };
    }


    public async Task<(bool Success, string Message)> UpdatePermissionsAsync(int roleId, int companyId,
        string[] selectedPermissions)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.CompanyId == companyId);

        if (role is null) return (false, "Role not found");

        var roleClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in roleClaims)
            await _roleManager.RemoveClaimAsync(role, claim);

        foreach (var permission in selectedPermissions)
            await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

        return (true, "Permissions updated successfully");
    }
}