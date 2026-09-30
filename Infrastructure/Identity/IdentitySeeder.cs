using Lab5.Application.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab5.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { AuthRoles.Admin, AuthRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.NewGuid() });
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to create role {Role}: {Errors}",
                        roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminEmail = configuration["Identity:AdminEmail"] ?? "admin@local.test";
        adminEmail = adminEmail.Trim().ToLowerInvariant();
        var adminPassword = configuration["Identity:AdminPassword"] ?? "Admin#12345";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Failed to create admin user: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Seeded admin identity user {Email}.", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, AuthRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, AuthRoles.Admin);
        }
    }
}
