namespace Lab5.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    // Navigation property
    public Category? Category { get; private set; }

    // Required by EF Core
    private Product() { }

    public Product(Guid categoryId, string name, string description, decimal price, int stockQuantity, string sku = "")
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Product price cannot be negative.", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
        Sku = string.IsNullOrWhiteSpace(sku)
            ? $"SKU-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}"
            : sku.Trim().ToUpperInvariant();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string description, decimal price, int stockQuantity, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Product price cannot be negative.", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Valid CategoryId is required.", nameof(categoryId));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));

        StockQuantity = quantity;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
