using Lab5.Domain.Entities;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (AppDbContext db) =>
        {
            try
            {
                var customers = await db.Customers
                    .AsNoTracking()
                    .Include(c => c.Orders)
                    .ToListAsync();

                var customerDtos = customers.Select(c => new CustomerDto(
                    c.Id,
                    c.Name,
                    c.Email,
                    c.CreatedAtUtc,
                    c.Orders.Count,
                    c.Orders.Sum(o => o.TotalAmount)))
                    .ToList();

                return Results.Ok(customerDtos);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve customers: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAllCustomers")
        .WithSummary("Get all customers with their order stats")
        .WithOpenApi();

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var customer = await db.Customers
                    .AsNoTracking()
                    .Include(c => c.Orders)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer is null)
                    return Results.NotFound(new { Message = $"Customer with ID {id} was not found." });

                var result = new CustomerDetailsDto(
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    customer.CreatedAtUtc,
                    customer.Orders.Select(o => new OrderSummaryDto(
                        o.Id,
                        o.TotalAmount,
                        o.Status,
                        o.Description,
                        o.OrderDateUtc)).ToList());

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve customer: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetCustomerById")
        .WithSummary("Get customer by ID with full order history")
        .WithOpenApi();

        group.MapPost("/", async (CreateCustomerRequest request, AppDbContext db) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
                    return Results.BadRequest(new { Message = "Name and Email are required." });

                var exists = await db.Customers.AnyAsync(c => c.Email == request.Email.Trim().ToLowerInvariant());
                if (exists)
                    return Results.Conflict(new { Message = "A customer with this email already exists." });

                var customer = new Customer(request.Name, request.Email);
                db.Customers.Add(customer);
                await db.SaveChangesAsync();

                return Results.Created($"/api/customers/{customer.Id}", new CustomerDto(
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    customer.CreatedAtUtc,
                    0,
                    0m));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create customer: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateCustomer")
        .WithSummary("Create a new customer")
        .WithOpenApi();

        return group;
    }
}

public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAtUtc,
    int OrdersCount,
    decimal TotalSpent);

public record CustomerDetailsDto(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAtUtc,
    List<OrderSummaryDto> Orders);

public record OrderSummaryDto(
    Guid Id,
    decimal TotalAmount,
    string Status,
    string Description,
    DateTime OrderDateUtc);

public record CreateCustomerRequest(string Name, string Email);
