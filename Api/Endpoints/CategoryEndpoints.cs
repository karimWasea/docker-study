using Lab5.Application.Auth;
using Lab5.Domain.Entities;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/categories").WithTags("Categories");

        // GET /api/categories
        group.MapGet("/", async (AppDbContext db) =>
        {
            try
            {
                var categories = await db.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .ToListAsync();

                var dtos = categories.Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CreatedAtUtc,
                    c.Products.Count))
                    .OrderBy(c => c.Name)
                    .ToList();

                return Results.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve categories: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAllCategories")
        .WithSummary("Get all product categories with product counts")
        .WithOpenApi()
        .RequireAuthorization();

        // GET /api/categories/{id}
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var category = await db.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category is null)
                    return Results.NotFound(new { Message = $"Category with ID {id} was not found." });

                var result = new CategoryDetailsDto(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.CreatedAtUtc,
                    category.Products.Select(p => new ProductSummaryDto(
                        p.Id,
                        p.Name,
                        p.Price,
                        p.StockQuantity,
                        p.Sku,
                        p.IsActive)).ToList());

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve category: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetCategoryById")
        .WithSummary("Get category by ID with its products")
        .WithOpenApi()
        .RequireAuthorization();

        // POST /api/categories
        group.MapPost("/", async (CreateCategoryRequest request, AppDbContext db) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Results.BadRequest(new { Message = "Category name is required." });

                var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower());
                if (exists)
                    return Results.Conflict(new { Message = $"A category named '{request.Name}' already exists." });

                var category = new Category(request.Name, request.Description ?? string.Empty);
                db.Categories.Add(category);
                await db.SaveChangesAsync();

                return Results.Created($"/api/categories/{category.Id}", new CategoryDto(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.CreatedAtUtc,
                    0));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create category: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateCategory")
        .WithSummary("Create a new product category")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        // PUT /api/categories/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, AppDbContext db) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Results.BadRequest(new { Message = "Category name is required." });

                var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
                if (category is null)
                    return Results.NotFound(new { Message = $"Category with ID {id} was not found." });

                var duplicateName = await db.Categories
                    .AnyAsync(c => c.Id != id && c.Name.ToLower() == request.Name.Trim().ToLower());
                if (duplicateName)
                    return Results.Conflict(new { Message = $"Another category named '{request.Name}' already exists." });

                category.Update(request.Name, request.Description ?? string.Empty);
                await db.SaveChangesAsync();

                return Results.Ok(new CategoryDto(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.CreatedAtUtc,
                    await db.Products.CountAsync(p => p.CategoryId == category.Id)));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to update category: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UpdateCategory")
        .WithSummary("Update category details")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        // DELETE /api/categories/{id}
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
                if (category is null)
                    return Results.NotFound(new { Message = $"Category with ID {id} was not found." });

                var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                    return Results.BadRequest(new { Message = "Cannot delete category that contains products. Reassign or delete products first." });

                db.Categories.Remove(category);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to delete category: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("DeleteCategory")
        .WithSummary("Delete a category")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        return group;
    }
}

public record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    int ProductsCount);

public record CategoryDetailsDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    List<ProductSummaryDto> Products);

public record ProductSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity,
    string Sku,
    bool IsActive);

public record CreateCategoryRequest(string Name, string? Description);
public record UpdateCategoryRequest(string Name, string? Description);
