namespace RetailCommerce.Application.Discounts;

/// <summary>Named discount campaigns (e.g. "14 August Sale — 15% off") managed in Settings and
/// offered as a picklist at POS. Percentage or fixed-amount — the POS terminal converts a
/// fixed amount into an equivalent cart-wide percent before submitting the sale, so the order
/// itself never needs to know which type was used (see CreateSaleRequest.DiscountLabel).</summary>
public interface IDiscountService
{
    Task<IReadOnlyList<DiscountDto>> ListAsync(bool activeOnly, CancellationToken ct = default);
    Task<DiscountDto> CreateAsync(UpsertDiscountRequest request, CancellationToken ct = default);
    Task<DiscountDto> UpdateAsync(Guid id, UpsertDiscountRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
