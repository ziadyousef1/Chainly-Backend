namespace Chainly.Application.DTOs.User;

public class UserUpdateDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public List<string>? Roles { get; set; }
}