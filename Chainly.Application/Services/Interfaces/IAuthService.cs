using Chainly.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.Application.Interfaces
{
    public interface IAuthService
    {
        Task<IActionResult> RegisterAsync(RegisterDto model);
        Task<IActionResult> LoginAsync(LoginDto model);
        Task<IActionResult> ForgotPasswordAsync(string email);
        Task<IActionResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task<IActionResult> VerifyEmailAsync(string email, string code);
        Task<IActionResult> ResendVerificationCodeAsync(string email);
    }
}
