using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Chainly.Data;
using Chainly.Domain.Constants;
using Chainly.Domain.Models;

namespace Chainly.Infrastructure.Helpers
{
    public static class DBInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (!context.Companies.Any())
                {
                    context.Companies.AddRange(
                        new Company
                        {
                            Name = "OpenAI",
                            LocationLatitude = (decimal)37.7749,
                            LocationLongitude = (decimal)-122.4194,
                            Logo = null
                        },
                        new Company
                        {
                            Name = "Microsoft",
                            LocationLatitude = (decimal)47.6062,
                            LocationLongitude = (decimal)-122.3321,
                            Logo = null
                        }
                    );
                    await context.SaveChangesAsync();
                }

                var role = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Manager" && r.CompanyId == 1);

                if (role == null)
                {
                    role = new Role { Name = "Manager", CompanyId = 1, NormalizedName = "MANAGER" };
                    context.Roles.Add(role);
                    await context.SaveChangesAsync();
                }

                var defaultUser = new User
                {
                    FullName = "Admin",
                    UserName = "admin",
                    Email = "admin@gmail.com",
                    EmailConfirmed = true,
                    CompanyId = 1,
                };

                var user = await userManager.FindByEmailAsync(defaultUser.Email);

                if (user is null)
                {
                    await userManager.CreateAsync(defaultUser, "P@ssword123");
                    await userManager.AddToRolesAsync(defaultUser, new List<string>
                    {
                        Roles.Manager.ToString(),
                    });
                }

                await roleManager.SeedAllPermissions(role);
            }
        }

        public static async Task SeedAllPermissions(this RoleManager<Role> roleManager, Role role)
        {
            if (role == null) return;

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var allPermissions = Permissions.GenerateAllPermissions();

            foreach (var modulePermissions in allPermissions)
            {
                foreach (var permission in modulePermissions.Value)
                {
                    if (!existingClaims.Any(c => c.Type == CustomClaimTypes.Permission && c.Value == permission))
                    {
                        await roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
                    }
                }
            }
        }
    }
}
