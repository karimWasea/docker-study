namespace Lab5.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string Description { get; private set; } = string.Empty;
    public DateTime OrderDateUtc { get; private set; }

    // Navigation property
    public Customer? Customer { get; private set; }

    // Required by EF Core
    private Order() { }

    public Order(Guid customerId, decimal totalAmount, string description = "")
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (totalAmount <= 0)
            throw new ArgumentException("Total amount must be greater than zero.", nameof(totalAmount));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Description = description.Trim();
        Status = "Pending";
        OrderDateUtc = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = "Completed";
    }

    public void Cancel()
    {
        Status = "Cancelled";
    }
}
