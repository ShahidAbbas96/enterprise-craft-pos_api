using Microsoft.EntityFrameworkCore;
using RetailCommerce.Application.Settings;
using RetailCommerce.Domain.Catalog;
using RetailCommerce.Infrastructure.Persistence;

namespace RetailCommerce.Infrastructure.Settings;

public class PosSettingsService(AppDbContext db) : IPosSettingsService
{
    public async Task<PosSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var entity = await db.PosSettings.FirstOrDefaultAsync(ct);
        if (entity is null)
        {
            entity = new PosSettings();
            db.PosSettings.Add(entity);
            await db.SaveChangesAsync(ct);
        }
        return ToDto(entity);
    }

    public async Task<PosSettingsDto> UpdateAsync(UpdatePosSettingsRequest request, CancellationToken ct = default)
    {
        var entity = await db.PosSettings.FirstOrDefaultAsync(ct);
        if (entity is null)
        {
            entity = new PosSettings();
            db.PosSettings.Add(entity);
        }
        entity.ViewMode = request.ViewMode;
        entity.ReturnPolicyDays = request.ReturnPolicyDays;
        entity.VisibleProductFields = string.Join(',', request.VisibleProductFields);
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private static PosSettingsDto ToDto(PosSettings e) => new(
        e.ViewMode,
        e.ReturnPolicyDays,
        string.IsNullOrWhiteSpace(e.VisibleProductFields) ? [] : e.VisibleProductFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
