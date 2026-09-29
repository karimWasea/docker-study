using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
    Description = "Web API that communicates with the browser",
    OperatingSystem = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.OSArchitecture.ToString(),
    Framework = RuntimeInformation.FrameworkDescription,
    ContainerHost = Environment.MachineName,
    CurrentTimeUtc = DateTime.UtcNow
}))
.WithName("GetSystemInfo")
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
