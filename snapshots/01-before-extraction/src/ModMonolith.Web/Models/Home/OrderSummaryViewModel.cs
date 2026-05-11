namespace ModMonolith.Web.Models.Home;

public sealed class OrderSummaryViewModel
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public decimal TotalAmount { get; set; }

    public IReadOnlyList<OrderLineSummaryViewModel> Lines { get; set; } = [];
}
