using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Settings;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(ICurrencySettingsService currencySettingsService, IPosSettingsService posSettingsService) : ControllerBase
{
    [HttpGet("currency")]
    public async Task<ActionResult<CurrencySettingsDto>> GetCurrency(CancellationToken ct)
    {
        return Ok(await currencySettingsService.GetAsync(ct));
    }

    [HttpPut("currency")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<CurrencySettingsDto>> UpdateCurrency(UpdateCurrencySettingsRequest request, CancellationToken ct)
    {
        return Ok(await currencySettingsService.UpdateAsync(request, ct));
    }

    [HttpGet("pos")]
    public async Task<ActionResult<PosSettingsDto>> GetPos(CancellationToken ct)
    {
        return Ok(await posSettingsService.GetAsync(ct));
    }

    [HttpPut("pos")]
    [Authorize(Policy = "CatalogManagers")]
    public async Task<ActionResult<PosSettingsDto>> UpdatePos(UpdatePosSettingsRequest request, CancellationToken ct)
    {
        return Ok(await posSettingsService.UpdateAsync(request, ct));
    }
}
