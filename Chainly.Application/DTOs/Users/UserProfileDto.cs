namespace Chainly.Application.DTOs.Users;

public class UserProfileDto
{
    public string ProfilePicture { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}