namespace Chainly.Application.DTOs.Role;

public class RoleUpdatePermissionsDto
{
    public string[] SelectedPermissions { get; set; } = Array.Empty<string>();
}