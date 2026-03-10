using Chainly.Infrastructure.Extensions;
using Chainly.Infrastructure.Helpers;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddAuthorizationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();
await DBInitializer.SeedAsync(app.Services);

app.UseStaticFiles();
app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference(options => { options.Theme = ScalarTheme.Mars; });

app.Run();