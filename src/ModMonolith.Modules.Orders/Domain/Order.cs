namespace ModMonolith.Modules.Orders.Domain;

public sealed class Order
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}
