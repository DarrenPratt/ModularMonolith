using ModMonolith.Shared.Contracts.Catalog;

namespace ModMonolith.Web.Models.Home;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<string> Modules { get; set; } = [];

    public IReadOnlyList<ProductSummary> Products { get; set; } = [];

    public IReadOnlyList<OrderSummaryViewModel> Orders { get; set; } = [];

    public CreateProductInputModel ProductForm { get; set; } = new();

    public CreateOrderInputModel OrderForm { get; set; } = new();

    public string? StatusMessage { get; set; }

    public string? StatusType { get; set; }

    public string? DatabaseUnavailableMessage { get; set; }
}
