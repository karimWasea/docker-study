using Lab5.Domain.Entities;
using Lab5.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lab5.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        int maxRetries = isContainer ? 5 : 2;
        var delay = TimeSpan.FromSeconds(isContainer ? 2 : 1);

        bool initSucceeded = false;
        var provider = context.Database.ProviderName ?? "Unknown";

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting database schema initialization with provider {Provider} (attempt {Attempt}/{MaxRetries})...", provider, attempt, maxRetries);

                if (context.Database.IsSqlServer())
                {
                    try
                    {
                        await context.Database.MigrateAsync();
                    }
                    catch
                    {
                        await EnsureRelationalTablesCreatedAsync(context, logger);
                    }
                }
                else
                {
                    // PostgreSQL / SQLite: create tables directly from model definitions
                    await EnsureRelationalTablesCreatedAsync(context, logger);
                }

                logger.LogInformation("Database schema initialized successfully.");
                initSucceeded = true;
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database schema initialization failed on attempt {Attempt}/{MaxRetries}: {Message}. Retrying in {Delay}s...", attempt, maxRetries, ex.Message, delay.TotalSeconds);
                if (attempt == maxRetries)
                {
                    logger.LogWarning("⚠️ Could not initialize database after {MaxRetries} attempts. The API will continue running.", maxRetries);
                }
                else
                {
                    await Task.Delay(delay);
                }
            }
        }

        if (!initSucceeded)
        {
            return;
        }

        try
        {
            await IdentitySeeder.SeedAsync(scope.ServiceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Identity seed skipped: {Message}", ex.Message);
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
            var allOrders = await context.Orders.ToListAsync();
            var completedOrders = allOrders
                .Where(o => o.TotalAmount > 200m)
                .OrderByDescending(o => o.TotalAmount)
                .Take(5)
                .ToList();

            foreach (var order in completedOrders)
                order.MarkCompleted();

            var cancelledOrders = allOrders
                .Where(o => o.TotalAmount < 50m)
                .OrderBy(o => o.TotalAmount)
                .Take(2)
                .ToList();

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

        // Seed Categories and Products if empty
        try
        {
            if (!await context.Categories.AnyAsync())
            {
                logger.LogInformation("Seeding initial Categories and Products...");

                var electronics = new Category("Electronics", "Gadgets, audio, computing and smart devices");
                electronics.AddProduct("Wireless Noise-Cancelling Headphones", "High fidelity over-ear Bluetooth headphones with 30h battery", 199.99m, 45, "ELEC-HEAD-01");
                electronics.AddProduct("Mechanical Gaming Keyboard", "RGB backlit keyboard with tactile blue switches", 79.99m, 80, "ELEC-KEYB-02");
                electronics.AddProduct("Ultra-Fast 1TB Portable SSD", "USB 3.2 Gen 2 external solid-state drive with 1050MB/s", 119.50m, 60, "ELEC-SSD-03");
                electronics.AddProduct("27-inch 4K UHD IPS Monitor", "144Hz refresh rate with HDR 400 support and USB-C", 349.00m, 25, "ELEC-MON-04");

                var homeOffice = new Category("Home & Office", "Ergonomic furniture, lighting and desk accessories");
                homeOffice.AddProduct("Motorized Standing Desk", "Dual-motor electric height adjustable standing desk 55x28", 420.00m, 15, "HOME-DESK-01");
                homeOffice.AddProduct("Ergonomic Mesh Office Chair", "Breathable mesh back with adjustable 3D armrests", 249.90m, 30, "HOME-CHAIR-02");
                homeOffice.AddProduct("Smart LED Desk Lamp", "Dimmable desk lamp with wireless smartphone charging base", 45.00m, 100, "HOME-LAMP-03");

                var books = new Category("Books", "Technical, architectural and computer science literature");
                books.AddProduct("Clean Architecture by Robert C. Martin", "A craftsman's guide to software structure and design", 39.99m, 120, "BOOK-ARCH-01");
                books.AddProduct("Domain-Driven Design by Eric Evans", "Tackling complexity in the heart of software", 49.99m, 50, "BOOK-DDD-02");
                books.AddProduct("Designing Data-Intensive Applications", "The big ideas behind reliable, scalable systems", 44.50m, 90, "BOOK-DDIA-03");

                var fitness = new Category("Fitness & Lifestyle", "Workout equipment, wearables and active gear");
                fitness.AddProduct("Smart Fitness Watch", "Heart rate monitor with GPS tracking and 7-day battery", 159.00m, 40, "FIT-WATCH-01");
                fitness.AddProduct("Resistance Bands Training Set", "5 stackable resistance tubes with handles and door anchor", 29.99m, 150, "FIT-BAND-02");

                await context.Categories.AddRangeAsync(electronics, homeOffice, books, fitness);
                await context.SaveChangesAsync();

                var catCount = await context.Categories.CountAsync();
                var prodCount = await context.Products.CountAsync();
                logger.LogInformation("✅ Seeded {CategoryCount} categories and {ProductCount} products.", catCount, prodCount);
            }
            else
            {
                var catCount = await context.Categories.CountAsync();
                var prodCount = await context.Products.CountAsync();
                logger.LogInformation("Categories and products already seeded — {CategoryCount} categories, {ProductCount} products found.", catCount, prodCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not seed categories/products: {Message}", ex.Message);
        }
    }

    private static async Task EnsureRelationalTablesCreatedAsync(AppDbContext context, ILogger logger)
    {
        var databaseCreator = context.Database.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator;
        if (databaseCreator != null)
        {
            if (!await databaseCreator.ExistsAsync())
            {
                logger.LogInformation("Database does not exist. Creating database...");
                await databaseCreator.CreateAsync();
            }

            if (!await databaseCreator.HasTablesAsync())
            {
                logger.LogInformation("Creating database tables from entity models...");
                await databaseCreator.CreateTablesAsync();
            }
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }
}

