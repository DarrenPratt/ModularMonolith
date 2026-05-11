namespace ModMonolith.Shared.Contracts.Customers;

public interface ICustomerDirectory
{
    Task<IReadOnlyList<CustomerSummary>> GetAllAsync(CancellationToken cancellationToken);

    Task<CustomerSummary> CreateAsync(string name, string email, CancellationToken cancellationToken);
}
