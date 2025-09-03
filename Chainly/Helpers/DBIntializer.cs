namespace Chainly.Helpers
{
    public static class DBIntializer
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
                await SeedSuppliersFromExcelAsync(scope.ServiceProvider);
            }
        }

        public static async Task SeedSuppliersFromExcelAsync(IServiceProvider serviceProvider)
        {
            var supplierImportService = serviceProvider.GetService<ISupplierImportService>();
            if (supplierImportService == null) return;

            var excelFilePath = Path.Combine("suppliers.xlsx");

            if (File.Exists(excelFilePath))
            {
                var suppliers = await supplierImportService.ReadSuppliersFromExcelAsync(excelFilePath);
                var isValid = await supplierImportService.ValidateSupplierDataAsync(suppliers);

                if (isValid)
                {
                    await supplierImportService.ImportSuppliersAsync(suppliers, companyId: 1);
                }
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