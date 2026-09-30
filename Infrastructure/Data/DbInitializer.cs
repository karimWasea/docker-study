using Lab5.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        int maxRetries = isContainer ? 10 : 2;
        var delay = TimeSpan.FromSeconds(isContainer ? 3 : 1);

        bool migrationSucceeded = false;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");
                migrationSucceeded = true;
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database migration failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}s...", attempt, maxRetries, delay.TotalSeconds);
                if (attempt == maxRetries)
                {
                    logger.LogWarning("⚠️ Could not connect to SQL Server after {MaxRetries} attempts. If you are debugging locally in Visual Studio, ensure the database container is started using: 'docker compose up -d db'. The API will continue running.", maxRetries);
                }
                else
                {
                    await Task.Delay(delay);
                }
            }
        }

        if (!migrationSucceeded)
        {
            return;
        }

        // Seed data if database is empty
        if (!await context.Customers.AnyAsync())
        {
            logger.LogInformation("Seeding initial Customers and Orders...");

            // --- Customer 1: John Doe ---
            var john = new Customer("John Doe", "john.doe@example.com");
            john.AddOrder(150.50m, "Electronics - Wireless headphones");
            john.AddOrder(45.00m, "Books - Clean Architecture by Robert C. Martin");
            john.AddOrder(320.00m, "Smart home hub & accessories");

            // --- Customer 2: Alice Smith ---
            var alice = new Customer("Alice Smith", "alice.smith@example.com");
            alice.AddOrder(299.99m, "Ergonomic office chair");
            alice.AddOrder(89.90m, "Desk lamp with USB charging port");
            alice.AddOrder(1250.00m, "Standing desk - motorized");

            // --- Customer 3: Bob Johnson ---
            var bob = new Customer("Bob Johnson", "bob.johnson@example.com");
            bob.AddOrder(89.20m, "Wireless keyboard & mouse combo");
            bob.AddOrder(34.99m, "HDMI cable 2.1 - 2m");

            // --- Customer 4: Sara Williams ---
            var sara = new Customer("Sara Williams", "sara.williams@example.com");
            sara.AddOrder(499.00m, "Gaming monitor 27-inch 144Hz");
            sara.AddOrder(65.00m, "Mechanical keyboard - Blue switches");
            sara.AddOrder(39.99m, "Mouse pad XL");
            sara.AddOrder(189.00m, "Gaming headset with surround sound");

            // --- Customer 5: Mohamed Ali ---
            var mohamed = new Customer("Mohamed Ali", "m.ali@example.com");
            mohamed.AddOrder(720.00m, "Laptop cooling pad + docking station");
            mohamed.AddOrder(120.00m, "External SSD 1TB");

            // --- Customer 6: Lena Schmidt ---
            var lena = new Customer("Lena Schmidt", "lena.schmidt@example.com");
            lena.AddOrder(55.00m, "Notebook & stationery bundle");
            lena.AddOrder(799.99m, "Tablet 10-inch with stylus");
            lena.AddOrder(29.99m, "Screen cleaning kit");

            // --- Customer 7: Carlos Rivera ---
            var carlos = new Customer("Carlos Rivera", "carlos.rivera@example.com");
            carlos.AddOrder(219.00m, "Smart watch fitness edition");
            carlos.AddOrder(78.50m, "Resistance bands set & yoga mat");

            // --- Customer 8: Yuki Tanaka ---
            var yuki = new Customer("Yuki Tanaka", "yuki.tanaka@example.com");
            yuki.AddOrder(3200.00m, "Professional camera body DSLR");
            yuki.AddOrder(450.00m, "Camera lens 50mm f/1.8");
            yuki.AddOrder(95.00m, "Camera bag & tripod");
            yuki.AddOrder(24.99m, "Memory card 128GB class 10");

            await context.Customers.AddRangeAsync(john, alice, bob, sara, mohamed, lena, carlos, yuki);
            await context.SaveChangesAsync();

            // Mark a few orders with different statuses for realistic data
            var completedOrders = await context.Orders
                .Where(o => o.TotalAmount > 200m)
                .OrderByDescending(o => o.TotalAmount)
                .Take(5)
                .ToListAsync();

            foreach (var order in completedOrders)
                order.MarkCompleted();

            var cancelledOrders = await context.Orders
                .Where(o => o.TotalAmount < 50m)
                .OrderBy(o => o.TotalAmount)
                .Take(2)
                .ToListAsync();

            foreach (var order in cancelledOrders)
                order.Cancel();

            await context.SaveChangesAsync();

            var customerCount = await context.Customers.CountAsync();
            var orderCount = await context.Orders.CountAsync();
            logger.LogInformation(
                "✅ Initial database seeding completed. {CustomerCount} customers and {OrderCount} orders created.",
                customerCount, orderCount);
        }
        else
        {
            var customerCount = await context.Customers.CountAsync();
            var orderCount = await context.Orders.CountAsync();
            logger.LogInformation(
                "Database already seeded — {CustomerCount} customers, {OrderCount} orders found.",
                customerCount, orderCount);
        }
    }
}
