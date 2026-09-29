using System.Runtime.InteropServices;
using Lab5.Endpoints;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register EF Core DbContext with resilient SQL Server retries
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

var app = builder.Build();

// Run database migrations and seed data
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
app.MapGet("/api/info", () => Results.Ok(new
{
    Title = "🐳 ASP.NET Core 8 Web API running in Docker (Lab 5)",
    Description = "Web API that communicates with the browser and MSSQL",
    OperatingSystem = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.OSArchitecture.ToString(),
    Framework = RuntimeInformation.FrameworkDescription,
    ContainerHost = Environment.MachineName,
    CurrentTimeUtc = DateTime.UtcNow
}))
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
