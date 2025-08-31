

namespace Chainly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            return await _authService.RegisterAsync(model);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            return await _authService.LoginAsync(model);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> GenerateOTP([FromBody] GenerateOtpRequest otpRequest)
        {
            return await _authService.ForgotPasswordAsync(otpRequest.Email);
        }

        [HttpPost("validate-otp")]
        public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequest request)
        {
            return await _authService.VerifyEmailAsync(request.Email, request.Otp);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            return await _authService.ResetPasswordAsync(request.Email, request.Token, request.newPassword);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            return await _authService.ResendVerificationCodeAsync(request.Email);
        }
    }
}