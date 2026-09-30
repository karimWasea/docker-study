using Lab5.Domain.Entities;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", async (AppDbContext db) =>
        {
            try
            {
                var orders = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDateUtc)
                    .Select(o => new OrderDto(
                        o.Id,
                        o.CustomerId,
                        o.Customer != null ? o.Customer.Name : "Unknown",
                        o.Customer != null ? o.Customer.Email : "Unknown",
                        o.TotalAmount,
                        o.Status,
                        o.Description,
                        o.OrderDateUtc))
                    .ToListAsync();

                return Results.Ok(orders);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve orders: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAllOrders")
        .WithSummary("Get all orders with customer details")
        .WithOpenApi();

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var order = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order is null)
                    return Results.NotFound(new { Message = $"Order with ID {id} was not found." });

                var result = new OrderDto(
                    order.Id,
                    order.CustomerId,
                    order.Customer?.Name ?? "Unknown",
                    order.Customer?.Email ?? "Unknown",
                    order.TotalAmount,
                    order.Status,
                    order.Description,
                    order.OrderDateUtc);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve order: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetOrderById")
        .WithSummary("Get order by ID")
        .WithOpenApi();

        group.MapPost("/", async (CreateOrderRequest request, AppDbContext db) =>
        {
            try
            {
                if (request.CustomerId == Guid.Empty)
                    return Results.BadRequest(new { Message = "Valid CustomerId is required." });
                if (request.TotalAmount <= 0)
                    return Results.BadRequest(new { Message = "Total amount must be greater than zero." });

                var customer = await db.Customers.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == request.CustomerId);
                if (customer is null)
                    return Results.NotFound(new { Message = $"Customer with ID {request.CustomerId} does not exist." });

                // DDD: Add order through Customer aggregate root
                var order = customer.AddOrder(request.TotalAmount, request.Description ?? string.Empty);
                await db.SaveChangesAsync();

                return Results.Created($"/api/orders/{order.Id}", new OrderDto(
                    order.Id,
                    order.CustomerId,
                    customer.Name,
                    customer.Email,
                    order.TotalAmount,
                    order.Status,
                    order.Description,
                    order.OrderDateUtc));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create order: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateOrder")
        .WithSummary("Create a new order for a customer")
        .WithOpenApi();

        return group;
    }
}

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    string Status,
    string Description,
    DateTime OrderDateUtc);

public record CreateOrderRequest(Guid CustomerId, decimal TotalAmount, string? Description);
