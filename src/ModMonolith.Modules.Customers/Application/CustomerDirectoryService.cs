using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Customers.Domain;
using ModMonolith.Shared.Contracts.Customers;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Modules.Customers.Application;

public sealed class CustomerDirectoryService(ModMonolithDbContext dbContext) : ICustomerDirectory
{
    public async Task<IReadOnlyList<CustomerSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Customer>()
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerSummary(
                customer.Id,
                customer.Name,
                customer.Email,
                customer.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerSummary> CreateAsync(string name, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();

        var exists = await dbContext.Set<Customer>()
            .AnyAsync(customer => customer.Email == normalizedEmail, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A customer with email '{normalizedEmail}' already exists.");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = normalizedEmail,
            CreatedUtc = DateTime.UtcNow
        };

        dbContext.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerSummary(customer.Id, customer.Name, customer.Email, customer.CreatedUtc);
    }
}
