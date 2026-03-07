using Chainly.Application.Configurations;
using Chainly.Application.Interfaces;
using Chainly.Application.Services;
using Chainly.Application.Services.Interfaces;
using Chainly.Application.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAISettings"));
            services.Configure<AiApiSettings>(configuration.GetSection("AiApi"));
            services.Configure<CloudStorageSettings>(configuration.GetSection(CloudStorageSettings.AzureStorage));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAzureOpenAIService>(provider =>
            {
                var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
                var azureOpenAISettings = provider.GetRequiredService<IOptions<AzureOpenAISettings>>();
                return new AzureOpenAIService(azureOpenAISettings, httpClient);
            });
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ISupplierImportService, SupplierImportService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<ISupplierMaterialService, SupplierMaterialService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ISalesDataService, SalesDataService>();
            services.AddScoped<IProductionLineService, ProductionLineService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ILoggingService, LoggingService>();
            services.AddScoped<IMaterialService, MaterialService>();
            services.AddScoped<ICloudStorageService, CloudStorageService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ICameraService, CameraService>();

            services.AddHttpClient();
            services.AddScoped<IExternalApiService, ExternalApiService>();
            services.AddScoped<IAiInsightsService, AiInsightsService>();
            
            return services;
        }
    }
}