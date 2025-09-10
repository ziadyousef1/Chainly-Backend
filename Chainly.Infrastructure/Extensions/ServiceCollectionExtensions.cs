using Chainly.Data;
using Chainly.Domain.Models;
using Chainly.Infrastructure.MailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chainly.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development
                    ? configuration.GetConnectionString("DefaultConnection")
                    : configuration.GetConnectionString("ProductionConnection");

                options.UseSqlServer(connectionString);
            });

            services.AddIdentity<User, Role>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            var emailConfig = configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            if (emailConfig != null)
            {
                services.AddSingleton(emailConfig);
                services.AddTransient<IEmailSender, EmailSender>();
            }

            services.Configure<SecurityStampValidatorOptions>(options => 
            { 
                options.ValidationInterval = TimeSpan.Zero; 
            });

            return services;
        }
    }
}
