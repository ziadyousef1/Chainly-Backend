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

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        return await authService.LoginAsync(model);
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
}