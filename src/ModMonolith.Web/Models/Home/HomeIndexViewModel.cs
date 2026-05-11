using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Contracts.Customers;

namespace ModMonolith.Web.Models.Home;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<string> Modules { get; set; } = [];

    public IReadOnlyList<CustomerSummary> Customers { get; set; } = [];

    public IReadOnlyList<ProductSummary> Products { get; set; } = [];

    public IReadOnlyList<OrderSummaryViewModel> Orders { get; set; } = [];

    public CreateCustomerInputModel CustomerForm { get; set; } = new();

    public CreateProductInputModel ProductForm { get; set; } = new();

    public CreateOrderInputModel OrderForm { get; set; } = new();

    public string? StatusMessage { get; set; }

    public string? StatusType { get; set; }

    public string? DependencyUnavailableMessage { get; set; }
}
