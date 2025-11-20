using Microsoft.AspNetCore.Http;

namespace Chainly.Application.DTOs.Auth.Customer;

public class RegisterCustomerDto
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}