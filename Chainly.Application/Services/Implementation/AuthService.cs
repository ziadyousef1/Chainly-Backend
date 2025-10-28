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
using System.Text;
using Chainly.Application.Configurations;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Application.Dtos;
using Chainly.Application.Services.Interfaces;
using Chainly.Domain.Models;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services
{
    public class AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration config,
        AppDbContext context,
        IEmailSender emailSender,
        ICloudStorageService cloudStorageService,
        IOptions<CloudStorageSettings> cloudStorageSettings)
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
                    CompanyId = company.Id
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

        public async Task<IActionResult> LoginAsync(LoginDto model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
                return new UnauthorizedObjectResult("Invalid login attempt");

            if (!await userManager.IsEmailConfirmedAsync(user))
                return new UnauthorizedObjectResult(
                    "Email is not confirmed. Please verify your email before logging in.");

            var token = await GenerateJwtToken(user);
            return new OkObjectResult(new { Token = token });
        }

        public async Task<IActionResult> ForgotPasswordAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return new NotFoundObjectResult("User Not Found !");

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

        public async Task<IActionResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return new NotFoundObjectResult("User Not Found !");

            var result = await userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                return new BadRequestObjectResult(result.Errors);

            return new OkObjectResult("Password reset successfully!");
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

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtSettings = config.GetSection("Jwt");

            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("companyId", user.CompanyId.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

                var roleEntity = await roleManager.Roles
                    .FirstOrDefaultAsync(r => r.Name == role && r.CompanyId == user.CompanyId);

                if (roleEntity != null)
                {
                    var roleClaims = await roleManager.GetClaimsAsync(roleEntity);
                    claims.AddRange(roleClaims
                        .Where(c => c.Type == CustomClaimTypes.Permission)
                        .Select(c => new Claim(CustomClaimTypes.Permission, c.Value)));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(Convert.ToInt32(jwtSettings["DurationInDays"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}