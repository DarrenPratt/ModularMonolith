namespace ModMonolith.Modules.Customers.Domain;

public sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}
