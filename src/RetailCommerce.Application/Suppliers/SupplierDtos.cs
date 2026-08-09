using RetailCommerce.Application.Common;

namespace RetailCommerce.Application.Suppliers;

public record SupplierDto(
    Guid Id, string Name, string? ContactName, string? Email, string? Phone,
    decimal Rating, decimal Balance, int LeadDays, string Status, DateTimeOffset CreatedAtUtc);

public record UpsertSupplierRequest(
    string Name, string? ContactName, string? Email, string? Phone,
    decimal Rating, int LeadDays, string Status);

public class SupplierListQuery : PagedQuery
{
    public string? Status { get; set; }
}
