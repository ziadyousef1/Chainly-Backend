using Chainly.Application.Configurations;
using Chainly.Application.Interfaces;
using Chainly.Application.Services;
using Chainly.Application.Services.Interfaces;
using Chainly.Application.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chainly.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ISupplierImportService, SupplierImportService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<ISupplierMaterialService, SupplierMaterialService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ISalesDataService, SalesDataService>();
            services.AddScoped<IProductionLineService, ProductionLineService>();
            return services;
        }
    }
}