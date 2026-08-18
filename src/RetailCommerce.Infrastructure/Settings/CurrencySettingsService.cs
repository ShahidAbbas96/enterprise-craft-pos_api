using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Settings;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Settings;

public class CurrencySettingsService(AppDbContext db) : ICurrencySettingsService
{
    public async Task<CurrencySettingsDto> GetAsync(CancellationToken ct = default)
    {
        var entity = await db.CurrencySettings.FirstOrDefaultAsync(ct);
        if (entity is null)
        {
            entity = new CurrencySettings();
            db.CurrencySettings.Add(entity);
            await db.SaveChangesAsync(ct);
        }
        return ToDto(entity);
    }

    public async Task<CurrencySettingsDto> UpdateAsync(UpdateCurrencySettingsRequest request, CancellationToken ct = default)
    {
        var entity = await db.CurrencySettings.FirstOrDefaultAsync(ct);
        if (entity is null)
        {
            entity = new CurrencySettings();
            db.CurrencySettings.Add(entity);
        }
        entity.Symbol = request.Symbol.Trim();
        entity.DecimalPlaces = request.DecimalPlaces;
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private static CurrencySettingsDto ToDto(CurrencySettings e) => new(e.Symbol, e.DecimalPlaces);
}
