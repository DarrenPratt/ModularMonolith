namespace ModMonolith.Shared.Contracts.Customers;

public sealed record CustomerSummary(Guid Id, string Name, string Email, DateTime CreatedUtc);
