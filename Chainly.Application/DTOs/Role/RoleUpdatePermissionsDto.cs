namespace Chainly.Application.DTOs.Role;

public class RoleUpdatePermissionsDto
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, List<PermissionDto>> Permissions { get; set; } = new();
}