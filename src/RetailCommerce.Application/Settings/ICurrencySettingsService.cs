namespace RetailCommerce.Application.Settings;

public interface ICurrencySettingsService
{
    Task<CurrencySettingsDto> GetAsync(CancellationToken ct = default);

    Task<CurrencySettingsDto> UpdateAsync(UpdateCurrencySettingsRequest request, CancellationToken ct = default);
}
