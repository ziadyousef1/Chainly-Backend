using Chainly.Infrastructure.Extensions;
using Chainly.Infrastructure.Helpers;
using Chainly.Infrastructure.Hubs;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAuthorizationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();
await DBInitializer.SeedAsync(app.Services);

app.UseStaticFiles();
app.UseGlobalExceptionHandling();
app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ReportHub>("/hubs/reports");
app.MapOpenApi();
app.MapScalarApiReference(options => { options.Theme = ScalarTheme.Mars; });

app.Run();