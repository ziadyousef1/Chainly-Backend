
using Chainly.Application.Interfaces;
using Chainly.Application.Services;
using Chainly.Application.Services.Interfaces;
using Chainly.Application.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Chainly.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISupplierImportService, SupplierImportService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRoleService, RoleService>();

            return services;
        }
    }
}
