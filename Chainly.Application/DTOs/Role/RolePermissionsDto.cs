namespace Chainly.Application.DTOs.Role;

public class RolePermissionsDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Dictionary<string, List<PermissionDto>> Permissions { get; set; } = new();

}