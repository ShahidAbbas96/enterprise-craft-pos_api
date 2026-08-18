namespace RetailCommerce.Application.Settings;

public interface IPosSettingsService
{
    Task<PosSettingsDto> GetAsync(CancellationToken ct = default);

    Task<PosSettingsDto> UpdateAsync(UpdatePosSettingsRequest request, CancellationToken ct = default);
}
