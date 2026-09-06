using Chainly.Application.Interfaces;
using Chainly.Infrastructure.MailService;
using Chainly.Infrastructure.OTP_Validation;
using Chainly.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chainly.Application.Configurations;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Application.Dtos;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Chainly.Application.Dtos.Auth;
using Chainly.Application.DTOs.Auth.Customer;

namespace Chainly.Application.Services.Implementation;

public class AuthService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IConfiguration config,
    AppDbContext context,
    IEmailSender emailSender,
    ICloudStorageService cloudStorageService,
    IOptions<CloudStorageSettings> cloudStorageSettings,
    IOptions<JwtSettings> jwtSettings)
    : IAuthService
{
    public async Task<IActionResult> RegisterAsync(RegisterDto model)
    {
        if (model.Password != model.ConfirmPassword)
            return new BadRequestObjectResult("Password and confirmation do not match");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var company = await context.Companies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.CompanyName.ToLower());

            if (company != null)
                return new BadRequestObjectResult(new { Message = "A company with this name already exists." });

            company = new Company
            {
                Name = model.CompanyName,
                LocationLatitude = model.LocationLatitude,
                LocationLongitude = model.LocationLongitude,
                Logo = null
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CompanyId = company.Id,
                IsActive = true,
            };

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var uploadResult = await cloudStorageService.UploadAsync(
                    cloudStorageSettings.Value.Containers.Avatars,
                    model.ProfilePicture,
                    $"{Guid.NewGuid()}"
                );

                if (!uploadResult.Error)
                {
                    user.ProfilePicture = uploadResult.Blob.Uri;
                }
            }

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return new BadRequestObjectResult(result.Errors);

            var role = new Role { Name = "Manager", CompanyId = company.Id, NormalizedName = "MANAGER" };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            var allPermissions = Permissions.GenerateAllPermissions();
            foreach (var perm in allPermissions.SelectMany(kvp => kvp.Value))
            {
                await roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, perm));
            }


            context.UserRoles.Add(new IdentityUserRole<int>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await transaction.CommitAsync();

            await ResendVerificationCodeAsync(user.Email);

            return new OkObjectResult(new { Message = "User registered successfully" });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IActionResult> RegisterCustomerAsync(RegisterCustomerDto model)
    {
        if (model.Password != model.ConfirmPassword)
            return new BadRequestObjectResult("Password and confirmation do not match");

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            return new BadRequestObjectResult("Email is already registered.");

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.Phone,
        };

        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            var uploadResult = await cloudStorageService.UploadAsync(
                cloudStorageSettings.Value.Containers.Avatars,
                model.ProfilePicture,
                $"{Guid.NewGuid()}"
            );

            if (!uploadResult.Error)
                user.ProfilePicture = uploadResult.Blob.Uri;
        }

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);


        await context.SaveChangesAsync();

        await ResendVerificationCodeAsync(user.Email);

        return new OkObjectResult(new { Message = "Customer registered successfully" });
    }

    public async Task<IActionResult> LoginAsync(LoginDto model, bool isCustomer)
    {
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, model.Password) || (isCustomer && user.CompanyId is not null) || (!isCustomer && user.CompanyId is null))
            return new UnauthorizedObjectResult("Invalid login attempt");

        if (!user.IsActive)
        {
            return new UnauthorizedObjectResult("User is not active");  
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
            return new UnauthorizedObjectResult(
                "Email is not confirmed. Please verify your email before logging in.");

        var token = await GenerateJwtToken(user, isCustomer);

        var response = new LoginResponseDto();

        response.Token = token;
        response.fullName = user.FullName;

        int refreshTokenDays = model.RememberMe
            ? jwtSettings.Value.RefreshTokenDurationInDays * 2
            : jwtSettings.Value.RefreshTokenDurationInDays;

        if (user.RefreshTokens.Any(t => t.IsActive))
        {
            var activeRefreshToken = user.RefreshTokens.First(t => t.IsActive);
            response.RefreshToken = activeRefreshToken.Token;
            response.RefreshTokenExpiration = activeRefreshToken.ExpiresOn;
        }
        else
        {
            var refreshToken = GenerateRefreshToken(refreshTokenDays);
            response.RefreshToken = refreshToken.Token;
            response.RefreshTokenExpiration = refreshToken.ExpiresOn;
            user.RefreshTokens.Add(refreshToken);
            await userManager.UpdateAsync(user);
        }

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> ForgotPasswordAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return new NotFoundObjectResult("User Not Found !");

        if (IsPasswordChangeLocked(user))
            return new ObjectResult("Password reset is disabled for the demo account.") { StatusCode = 403 };

        var code = new Random().Next(100000, 999999).ToString();
        user.VerificationCode = code;
        user.ExpirationCode = DateTime.UtcNow.AddMinutes(10);

        await userManager.UpdateAsync(user);

        var message = new EmailMessage(
            new List<string> { email },
            "OTP For Reset Password",
            HtmlTemplate.GetVerificationCodeEmailTemplate(code)
        );

        await emailSender.SendEmailAsync(message);

        return new OkObjectResult("OTP Sent Successfully :)");
    }

    public async Task<IActionResult> ResetPasswordAsync(int currentUserId, string email, string currentPassword,
        string newPassword, string newPasswordConfirm)
    {
        var user = await userManager.FindByIdAsync(currentUserId.ToString());
        if (user == null)
            return new NotFoundObjectResult("User Not Found!");

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            return new ObjectResult("You can only reset your own password.") { StatusCode = 403 };

        if (IsPasswordChangeLocked(user))
            return new ObjectResult("Password reset is disabled for the demo account.") { StatusCode = 403 };

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0 && !await UserHasPermissionAsync(user, roles, Permissions.Users.ResetPassword))
            return new ObjectResult("You do not have permission to reset password.") { StatusCode = 403 };

        if (newPassword != newPasswordConfirm)
        {
            return new BadRequestObjectResult("Passwords do not match");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
            return new BadRequestObjectResult(result.Errors);

        return new OkObjectResult("Password reset successfully!");
    }

    private static bool IsPasswordChangeLocked(User user) =>
        !string.IsNullOrEmpty(user.Email) && DemoAccounts.Emails.Contains(user.Email);

    private async Task<bool> UserHasPermissionAsync(User user, IList<string> roles, string permission)
    {
        var roleIds = await roleManager.Roles
            .Where(r => r.CompanyId == user.CompanyId && roles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        if (roleIds.Count == 0)
            return false;

        return await context.RoleClaims.AnyAsync(rc =>
            roleIds.Contains(rc.RoleId)
            && rc.ClaimType == CustomClaimTypes.Permission
            && rc.ClaimValue == permission);
    }

    public async Task<IActionResult> VerifyEmailAsync(string email, string code)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return new NotFoundObjectResult("User Not Found!");

        if (user.VerificationCode == code && user.ExpirationCode > DateTime.UtcNow)
        {
            user.VerificationCode = null;
            user.ExpirationCode = null;
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            return new OkObjectResult(new { Message = "OTP Verified Successfully!" });
        }

        return new BadRequestObjectResult("Invalid OR Expired OTP.");
    }

    public async Task<IActionResult> ResendVerificationCodeAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return new NotFoundObjectResult("User Not Found !");

        var newCode = new Random().Next(100000, 999999).ToString();
        user.VerificationCode = newCode;
        user.ExpirationCode = DateTime.UtcNow.AddMinutes(10);

        await userManager.UpdateAsync(user);

        var htmlBody = HtmlTemplate.GetVerificationCodeEmailTemplate(newCode);

        var message = new EmailMessage(
            new List<string> { user.Email },
            "Your New Verification Code",
            htmlBody
        );

        await emailSender.SendEmailAsync(message);

        return new OkObjectResult("OTP Resent Successfully :)");
    }

    private async Task<string> GenerateJwtToken(User user, bool isCustomer)
    {
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };

        if (!isCustomer)
        {
            claims.Add(new Claim("companyId", user.CompanyId.ToString()));
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var roleIds = await roleManager.Roles
                .Where(r => r.CompanyId == user.CompanyId && roles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            if (roleIds.Count > 0)
            {
                var permissions = await context.RoleClaims
                    .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == CustomClaimTypes.Permission)
                    .Select(rc => rc.ClaimValue)
                    .Where(v => v != null)
                    .Distinct()
                    .ToListAsync();

                claims.AddRange(permissions.Select(p => new Claim(CustomClaimTypes.Permission, p!)));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Value.Issuer,
            audience: jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.Now.AddDays(Convert.ToInt32(jwtSettings.Value.DurationInDays)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(int durationInDays)
    {
        var randomNumber = new byte[32];
        using var generator = new RNGCryptoServiceProvider();
        generator.GetBytes(randomNumber);
        return new RefreshToken()
        {
            Token = Convert.ToBase64String(randomNumber),
            ExpiresOn = DateTime.UtcNow.AddDays(durationInDays),
            CreatedOn = DateTime.UtcNow,
        };
    }

    public async Task<IActionResult> RefreshTokenAsync(string token, bool isCustomer)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user is null)
            return new UnauthorizedObjectResult("Invalid token");

        var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

        if (!refreshToken.IsActive)
        {
            return new UnauthorizedObjectResult("Inactive token");
        }

        refreshToken.RevokedOn = DateTime.UtcNow;

        var newRefreshToken = GenerateRefreshToken(jwtSettings.Value.RefreshTokenDurationInDays);
        user.RefreshTokens.Add(newRefreshToken);
        await userManager.UpdateAsync(user);

        var result = new LoginResponseDto()
        {
            fullName = user.UserName,
            Token = await GenerateJwtToken(user, isCustomer),
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiration = newRefreshToken.ExpiresOn,
        };

        return new OkObjectResult(result);
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user is null)
            return false;

        var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

        if (!refreshToken.IsActive)
            return false;

        refreshToken.RevokedOn = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        return true;
    }
}