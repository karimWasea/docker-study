namespace Lab5.Domain.Entities;

public class Category
{
    private readonly List<Product> _products = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    // Required by EF Core
    private Category() { }

    public Category(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Product AddProduct(string name, string description, decimal price, int stockQuantity, string sku = "")
    {
        var product = new Product(Id, name, description, price, stockQuantity, sku);
        _products.Add(product);
        return product;
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }
}
