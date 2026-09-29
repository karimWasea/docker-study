namespace Lab5.Domain.Entities;

public class Customer
{
    private readonly List<Order> _orders = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // Required by EF Core
    private Customer() { }

    public Customer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Customer email cannot be empty.", nameof(email));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Order AddOrder(decimal totalAmount, string description = "")
    {
        var order = new Order(Id, totalAmount, description);
        _orders.Add(order);
        return order;
    }

    public void UpdateProfile(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Customer email cannot be empty.", nameof(email));

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}
