namespace Chainly.Application.DTOs.Role;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, List<PermissionDto>> Permissions { get; set; } = new();
}