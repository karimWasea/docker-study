using System.Runtime.InteropServices;
using Lab5.Endpoints;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

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

var app = builder.Build();

app.UseExceptionHandler();

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

// Weather Forecast endpoint
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Customer and Order DDD Endpoints
app.MapCustomerEndpoints();
app.MapOrderEndpoints();

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
