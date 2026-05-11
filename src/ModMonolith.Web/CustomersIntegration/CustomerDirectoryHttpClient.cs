using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ModMonolith.Shared.Contracts.Customers;

namespace ModMonolith.Web.CustomersIntegration;

public sealed class CustomerDirectoryHttpClient(HttpClient httpClient) : ICustomerDirectory
{
    public async Task<IReadOnlyList<CustomerSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<CustomerSummary>>(
                   "/api/customers/customers",
                   cancellationToken)
               ?? [];
    }

    public async Task<CustomerSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/api/customers/customers/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CustomerSummary>(cancellationToken)
            ?? throw new InvalidOperationException("The Customers API returned an empty response.");
    }

    public async Task<CustomerSummary> CreateAsync(string name, string email, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/customers/customers",
            new CreateCustomerHttpRequest(name, email),
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CustomerSummary>(cancellationToken)
                ?? throw new InvalidOperationException("The Customers API returned an empty response.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(await ReadProblemMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
        throw new InvalidOperationException("The Customers API request did not complete successfully.");
    }

    private static async Task<string> ReadProblemMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var details = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        var firstMessage = details?.Errors.Values.SelectMany(messages => messages).FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstMessage)
            ? "The Customers API rejected the request."
            : firstMessage;
    }

    private sealed record CreateCustomerHttpRequest(string Name, string Email);
}
