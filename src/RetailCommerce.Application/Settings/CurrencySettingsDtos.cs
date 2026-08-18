namespace RetailCommerce.Application.Settings;

public record CurrencySettingsDto(string Symbol, int DecimalPlaces);

public record UpdateCurrencySettingsRequest(string Symbol, int DecimalPlaces);
