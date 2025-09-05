using Chainly.Application.Configurations;
using Chainly.Application.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureOpenAISettings>(builder.Configuration.GetSection("AzureOpenAISettings"));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(builder.Environment.IsDevelopment()
        ? "DefaultConnection"
        : "ProductionConnection")));

var emailconfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
builder.Services.AddSingleton(emailconfig);

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<RoleManager<Role>>();

builder.Services.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.Zero; });

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? ""))
        };
    });


var app = builder.Build();
await DBIntializer.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapOpenApi();
app.MapScalarApiReference(options => { options.Theme = ScalarTheme.Mars; });

app.Run();