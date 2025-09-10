namespace Chainly.Application.DTOs.Roles;

public class RoleUpdatePermissionsDto
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, List<PermissionDto>> Permissions { get; set; } = new();
}