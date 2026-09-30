using Lab5.Application.Auth;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/stats", async (AppDbContext db) =>
        {
            try
            {
                var totalCustomers = await db.Customers.CountAsync();
                var orders = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .ToListAsync();

                var totalOrders = orders.Count;
                var totalRevenue = orders
                    .Where(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.TotalAmount);
                var pendingOrders = orders
                    .Count(o => o.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
                var completedOrders = orders
                    .Count(o => o.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
                var averageOrderValue = totalOrders > 0
                    ? Math.Round(orders.Sum(o => o.TotalAmount) / totalOrders, 2)
                    : 0m;

                var topCustomers = orders
                    .GroupBy(o => new { o.CustomerId, Name = o.Customer != null ? o.Customer.Name : "Unknown" })
                    .Select(g => new TopCustomerDto(
                        g.Key.CustomerId,
                        g.Key.Name,
                        g.Count(),
                        g.Sum(o => o.TotalAmount)))
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(3)
                    .ToList();

                var stats = new EcommerceDashboardDto(
                    totalCustomers,
                    totalOrders,
                    totalRevenue,
                    averageOrderValue,
                    pendingOrders,
                    completedOrders,
                    topCustomers,
                    DateTime.UtcNow);

                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve dashboard stats: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetEcommerceDashboardStats")
        .WithSummary("Get e-commerce metrics: revenue, order volume, status breakdown and top buyers")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        return group;
    }
}

public record EcommerceDashboardDto(
    int TotalCustomers,
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int PendingOrders,
    int CompletedOrders,
    List<TopCustomerDto> TopCustomers,
    DateTime GeneratedAtUtc);

public record TopCustomerDto(
    Guid CustomerId,
    string CustomerName,
    int OrdersCount,
    decimal TotalSpent);
