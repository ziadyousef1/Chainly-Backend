using Chainly.Application.Dtos.Auth;
using Chainly.Application.DTOs.Auth.Customer;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Interfaces;

public interface IAuthService
{
    Task<IActionResult> RegisterAsync(RegisterDto model);
    Task<IActionResult> RegisterCustomerAsync(RegisterCustomerDto model);
    Task<IActionResult> LoginAsync(LoginDto model, bool isCustomer);
    Task<IActionResult> ForgotPasswordAsync(string email);

    Task<IActionResult> ResetPasswordAsync(int currentUserId, string email, string currentPassword, string newPassword,
        string newPasswordConfirm);

    Task<IActionResult> VerifyEmailAsync(string email, string code);
    Task<IActionResult> ResendVerificationCodeAsync(string email);
    Task<IActionResult> RefreshTokenAsync(string token, bool isCustomer);
    Task<bool> RevokeTokenAsync(string token);
}