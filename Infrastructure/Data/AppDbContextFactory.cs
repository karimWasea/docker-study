using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lab5.Infrastructure.Data;

/// <summary>
/// Used by `dotnet ef` so migrations match SQL Server (Docker Compose), not the SQLite local fallback.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=AppDb;User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True");
        return new AppDbContext(optionsBuilder.Options);
    }
}
