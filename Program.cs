using System.Runtime.InteropServices;
using System.Text;
using Lab5.Application.Auth;
using Lab5.Endpoints;
using Lab5.Infrastructure.Data;
using Lab5.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lab 5 Web API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddProblemDetails();

// Enable CORS for Angular frontend running on port 4200
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Auto-detect database provider:
//   1. Railway / Cloud PostgreSQL: DATABASE_URL env var
//   2. Docker Compose / Remote SQL Server: ConnectionStrings__Default with non-localhost server
//   3. Standalone Container / Fallback: SQLite
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        // Railway injects DATABASE_URL as postgres:// or postgresql:// URI
        var postgresConn = BuildPostgresConnectionString(databaseUrl);
        options.UseNpgsql(postgresConn, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    }
    else if (!string.IsNullOrWhiteSpace(connectionString) &&
             !connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
    {
        // Docker Compose ("Server=db,1433...") or remote SQL Server instance
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
    else
    {
        // Standalone container on cloud platforms (e.g. Railway without Postgres addon)
        // or local development without running SQL Server
        options.UseSqlite("Data Source=app_data.db");
    }
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireLowercase = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(AuthRoles.Admin));
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration skipped/fallback: {Message}", ex.Message);
    }
}

// Run database schema creation and seed data
await DbInitializer.InitializeDatabaseAsync(app.Services, app.Logger);

// Enable Swagger always for easy browser testing inside Docker
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab 5 Web API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to Swagger UI for browser users
app.MapGet("/", () => Results.Redirect("/swagger"));

// Container and system diagnostic endpoint
app.MapGet("/api/info", async (AppDbContext db) =>
{
    string dbStatus = "Unknown";
    try
    {
        int count = await db.Customers.CountAsync();
        dbStatus = $"Connected ({count} customers)";
    }
    catch (Exception ex)
    {
        dbStatus = $"Error: {ex.Message}";
    }

    return Results.Ok(new
    {
        Title = "🐳 ASP.NET Core 8 Web API running in Docker (Lab 5)",
        Description = "Web API that communicates with the browser and resilient database",
        DatabaseProvider = db.Database.ProviderName,
        DatabaseStatus = dbStatus,
        OperatingSystem = RuntimeInformation.OSDescription,
        Architecture = RuntimeInformation.OSArchitecture.ToString(),
        Framework = RuntimeInformation.FrameworkDescription,
        ContainerHost = Environment.MachineName,
        CurrentTimeUtc = DateTime.UtcNow
    });
})
.WithName("GetSystemInfo")
.WithOpenApi();

// Environment greeting endpoint
app.MapGet("/greet", () => 
    $"Hello, {Environment.GetEnvironmentVariable("APP_GREETING") ?? "World"}! Running in {app.Environment.EnvironmentName} mode.")
.WithName("GetGreeting")
.WithOpenApi();


// Auth, Customer, Order, Catalog and Dashboard DDD Endpoints
app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapOrderEndpoints();
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapDashboardEndpoints();

app.Run();

// Helper to convert Railway / Heroku postgres:// or postgresql:// URL into Npgsql connection string
static string BuildPostgresConnectionString(string url)
{
    if (string.IsNullOrWhiteSpace(url)) return url;
    if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return url;
    }

    try
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
    }
    catch
    {
        return url;
    }
}
