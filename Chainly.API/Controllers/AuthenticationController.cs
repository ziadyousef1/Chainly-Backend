using Chainly.Application.Dtos.Auth;
using Chainly.Application.DTOs.Auth;
using Chainly.Application.DTOs.Auth.Customer;

namespace Chainly.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        return await authService.RegisterAsync(model);
    }

    [HttpPost("customer/register")]
    public async Task<IActionResult> Register(RegisterCustomerDto model)
    {
        return await authService.RegisterCustomerAsync(model);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        var result = await authService.LoginAsync(model, model.isCustomer);

        if (result is OkObjectResult { Value: LoginResponseDto payload } &&
            !string.IsNullOrEmpty(payload.RefreshToken))
        {
            SetRefreshTokenCookie(payload.RefreshToken, payload.RefreshTokenExpiration);
        }

        return result;
    }

    [HttpPost("forget-password")]
    public async Task<IActionResult> GenerateOTP([FromBody] GenerateOtpRequest otpRequest)
    {
        return await authService.ForgotPasswordAsync(otpRequest.Email);
    }

    [HttpPost("validate-otp")]
    public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequest request)
    {
        return await authService.VerifyEmailAsync(request.Email, request.Otp);
    }

    [HttpPost("reset-password")]
    [Authorize]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        return await authService.ResetPasswordAsync(request.Email, request.currentPassword, request.newPassword,
            request.newPasswordConfirm);
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        return await authService.ResendVerificationCodeAsync(request.Email);
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expires.ToLocalTime()
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    [HttpGet("refreshToken")]
    public async Task<IActionResult> RefreshToken(bool isCustomer)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var result = await authService.RefreshTokenAsync(refreshToken, isCustomer);

        if (result is OkObjectResult { Value: LoginResponseDto payload })
        {
            SetRefreshTokenCookie(payload.RefreshToken, payload.RefreshTokenExpiration);
        }

        return result;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeToken model)
    {
        var token = model.Token ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(token))
            return BadRequest("Token is required.");

        var result = await authService.RevokeTokenAsync(token);

        if (!result)
            return BadRequest("Token is invalid.");

        return Ok();
    }
}