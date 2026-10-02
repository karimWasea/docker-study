using Lab5.Application.Auth;
using Lab5.Domain.Entities;
using Lab5.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab5.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("Products");

        // GET /api/products (supports ?categoryId=... and ?search=...)
        group.MapGet("/", async (Guid? categoryId, string? search, AppDbContext db) =>
        {
            try
            {
                var query = db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .AsQueryable();

                if (categoryId.HasValue && categoryId.Value != Guid.Empty)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
                }

                var products = await query.ToListAsync();

                var dtos = products.Select(p => new ProductDto(
                    p.Id,
                    p.CategoryId,
                    p.Category != null ? p.Category.Name : "Uncategorized",
                    p.Name,
                    p.Description,
                    p.Price,
                    p.StockQuantity,
                    p.Sku,
                    p.IsActive,
                    p.CreatedAtUtc))
                    .OrderBy(p => p.Name)
                    .ToList();

                return Results.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve products: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAllProducts")
        .WithSummary("Get all products with category details and optional filters")
        .WithOpenApi()
        .RequireAuthorization();

        // GET /api/products/{id}
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var product = await db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product is null)
                    return Results.NotFound(new { Message = $"Product with ID {id} was not found." });

                var result = new ProductDto(
                    product.Id,
                    product.CategoryId,
                    product.Category != null ? product.Category.Name : "Uncategorized",
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.Sku,
                    product.IsActive,
                    product.CreatedAtUtc);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to retrieve product: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetProductById")
        .WithSummary("Get product details by ID")
        .WithOpenApi()
        .RequireAuthorization();

        // POST /api/products
        group.MapPost("/", async (CreateProductRequest request, AppDbContext db) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Results.BadRequest(new { Message = "Product name is required." });
                if (request.Price < 0)
                    return Results.BadRequest(new { Message = "Price cannot be negative." });
                if (request.StockQuantity < 0)
                    return Results.BadRequest(new { Message = "Stock quantity cannot be negative." });

                var category = await db.Categories.FindAsync(request.CategoryId);
                if (category is null)
                    return Results.NotFound(new { Message = $"Category with ID {request.CategoryId} does not exist." });

                // Check SKU uniqueness if provided
                if (!string.IsNullOrWhiteSpace(request.Sku))
                {
                    var skuExists = await db.Products.AnyAsync(p => p.Sku.ToUpper() == request.Sku.Trim().ToUpper());
                    if (skuExists)
                        return Results.Conflict(new { Message = $"A product with SKU '{request.Sku}' already exists." });
                }

                var product = new Product(
                    request.CategoryId,
                    request.Name,
                    request.Description ?? string.Empty,
                    request.Price,
                    request.StockQuantity,
                    request.Sku ?? string.Empty);

                db.Products.Add(product);
                await db.SaveChangesAsync();

                return Results.Created($"/api/products/{product.Id}", new ProductDto(
                    product.Id,
                    product.CategoryId,
                    category.Name,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.Sku,
                    product.IsActive,
                    product.CreatedAtUtc));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create product: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateProduct")
        .WithSummary("Create a new product under a category")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        // PUT /api/products/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, AppDbContext db) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Results.BadRequest(new { Message = "Product name is required." });
                if (request.Price < 0)
                    return Results.BadRequest(new { Message = "Price cannot be negative." });
                if (request.StockQuantity < 0)
                    return Results.BadRequest(new { Message = "Stock quantity cannot be negative." });

                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product is null)
                    return Results.NotFound(new { Message = $"Product with ID {id} was not found." });

                var category = await db.Categories.FindAsync(request.CategoryId);
                if (category is null)
                    return Results.NotFound(new { Message = $"Category with ID {request.CategoryId} does not exist." });

                product.Update(
                    request.Name,
                    request.Description ?? string.Empty,
                    request.Price,
                    request.StockQuantity,
                    request.CategoryId);

                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value) product.Activate();
                    else product.Deactivate();
                }

                await db.SaveChangesAsync();

                return Results.Ok(new ProductDto(
                    product.Id,
                    product.CategoryId,
                    category.Name,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.Sku,
                    product.IsActive,
                    product.CreatedAtUtc));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to update product: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UpdateProduct")
        .WithSummary("Update product details, pricing and category")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        // DELETE /api/products/{id}
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            try
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product is null)
                    return Results.NotFound(new { Message = $"Product with ID {id} was not found." });

                db.Products.Remove(product);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to delete product: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("DeleteProduct")
        .WithSummary("Delete a product")
        .WithOpenApi()
        .RequireAuthorization(AuthPolicies.Admin);

        return group;
    }
}

public record ProductDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Sku,
    bool IsActive,
    DateTime CreatedAtUtc);

public record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? Sku);

public record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    bool? IsActive);
