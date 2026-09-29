using Lab5.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const int maxRetries = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database migration failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}s...", attempt, maxRetries, delay.TotalSeconds);
                if (attempt == maxRetries)
                {
                    logger.LogError(ex, "Database migration failed after {MaxRetries} attempts.", maxRetries);
                    throw;
                }
                await Task.Delay(delay);
            }
        }

        // Seed data if database is empty
        if (!await context.Customers.AnyAsync())
        {
            logger.LogInformation("Seeding initial Customers and Orders...");

            var customer1 = new Customer("John Doe", "john.doe@example.com");
            customer1.AddOrder(150.50m, "First order - Electronics");
            customer1.AddOrder(45.00m, "Books and accessories");

            var customer2 = new Customer("Alice Smith", "alice.smith@example.com");
            customer2.AddOrder(299.99m, "Office chair");

            var customer3 = new Customer("Bob Johnson", "bob.johnson@example.com");
            customer3.AddOrder(89.20m, "Wireless keyboard & mouse");

            await context.Customers.AddRangeAsync(customer1, customer2, customer3);
            await context.SaveChangesAsync();

            logger.LogInformation("Initial database seeding completed.");
        }
    }
}
